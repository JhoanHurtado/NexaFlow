using Dapper;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository
{
    public class SaleRepository : ISaleRepository
    {
        private readonly string _connectionString;
        public SaleRepository(string connectionString) => _connectionString = connectionString;

        public async Task SaveAsync(Sale sale)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{sale.TenantId}'");

            using var tx = await conn.BeginTransactionAsync();
            await conn.ExecuteAsync(
                @"INSERT INTO sales (id, tenant_id, customer_id, reservation_id, total)
                  VALUES (@Id, @TenantId, @CustomerId, @ReservationId, @Total)",
                sale, tx);

            foreach (var item in sale.Items)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO sale_items (id, sale_id, product_id, quantity, unit_price)
                      VALUES (@Id, @SaleId, @ProductId, @Quantity, @UnitPrice)",
                    item, tx);
            }

            await tx.CommitAsync();
        }

        public async Task<SaleWithItems?> GetByIdAsync(Guid tenantId, Guid saleId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");

            var sale = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM sales WHERE id = @Id AND tenant_id = @TenantId",
                new { Id = saleId, TenantId = tenantId });

            if (sale is null) return null;

            var items = await conn.QueryAsync<dynamic>(
                @"SELECT si.*, p.name as product_name FROM sale_items si
                  JOIN products p ON p.id = si.product_id
                  WHERE si.sale_id = @SaleId",
                new { SaleId = saleId });

            return MapToSaleWithItems(sale, items);
        }

        public async Task<(IEnumerable<SaleWithItems> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");

            var sales = await conn.QueryAsync<dynamic>(
                @"SELECT *, count(*) OVER() as TotalCount FROM sales
                  WHERE tenant_id = @TId ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset",
                new { TId = tenantId, Limit = pageSize, Offset = (page - 1) * pageSize });

            var total = (int)(sales.FirstOrDefault()?.totalcount ?? 0);
            var saleIds = sales.Select(s => (Guid)s.id).ToList();

            var allItems = saleIds.Count == 0
                ? Enumerable.Empty<dynamic>()
                : await conn.QueryAsync<dynamic>(
                    @"SELECT si.*, p.name as product_name FROM sale_items si
                      JOIN products p ON p.id = si.product_id
                      WHERE si.sale_id = ANY(@Ids)",
                    new { Ids = saleIds.ToArray() });

            var itemsBySale = allItems.GroupBy(i => (Guid)i.sale_id)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            var result = sales.Select(s =>
            {
                itemsBySale.TryGetValue((Guid)s.id, out var saleItems);
                return MapToSaleWithItems(s, saleItems ?? Enumerable.Empty<dynamic>());
            }).Cast<SaleWithItems>().ToList();

            return (result, total);
        }

        private static SaleWithItems MapToSaleWithItems(dynamic s, IEnumerable<dynamic> items)
        {
            var sale = new Sale((Guid)s.tenant_id, (Guid?)s.customer_id, (Guid?)s.reservation_id);
            var mappedItems = items.Select(i => (
                ProductId: (Guid)i.product_id,
                ProductName: (string)i.product_name,
                Quantity: (int)i.quantity,
                UnitPrice: (decimal)i.unit_price
            ));
            return new SaleWithItems(sale, mappedItems);
        }
    }
}
