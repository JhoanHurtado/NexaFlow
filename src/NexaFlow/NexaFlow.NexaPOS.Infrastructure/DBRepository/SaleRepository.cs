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
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, sale.TenantId);
            await using var tx = await conn.BeginTransactionAsync();

            await using (var cmd = new NpgsqlCommand(
                @"INSERT INTO sales (id, tenant_id, customer_id, reservation_id, subtotal, tax_rate, tax_amount, total, status)
                  VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)", conn, tx))
            {
                cmd.Parameters.AddWithValue(sale.Id);
                cmd.Parameters.AddWithValue(sale.TenantId);
                cmd.Parameters.AddWithValue((object?)sale.CustomerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue((object?)sale.ReservationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue(sale.Subtotal);
                cmd.Parameters.AddWithValue(sale.TaxRate);
                cmd.Parameters.AddWithValue(sale.TaxAmount);
                cmd.Parameters.AddWithValue(sale.Total);
                cmd.Parameters.AddWithValue(sale.Status);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var item in sale.Items)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO sale_items (id, sale_id, product_id, quantity, unit_price) VALUES ($1, $2, $3, $4, $5)", conn, tx);
                cmd.Parameters.AddWithValue(item.Id);
                cmd.Parameters.AddWithValue(item.SaleId);
                cmd.Parameters.AddWithValue(item.ProductId);
                cmd.Parameters.AddWithValue(item.Quantity);
                cmd.Parameters.AddWithValue(item.UnitPrice);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<SaleWithItems?> GetByIdAsync(Guid tenantId, Guid saleId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);

            Sale? sale = null;
            await using (var cmd = new NpgsqlCommand(
                @"SELECT id, tenant_id, customer_id, reservation_id, subtotal, tax_rate, tax_amount, total, status, created_at
                  FROM sales WHERE id = $1 AND tenant_id = $2", conn))
            {
                cmd.Parameters.AddWithValue(saleId);
                cmd.Parameters.AddWithValue(tenantId);
                await using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;
                sale = Sale.Reconstitute(r.GetGuid(0), r.GetGuid(1),
                    r.IsDBNull(2) ? null : r.GetGuid(2),
                    r.IsDBNull(3) ? null : r.GetGuid(3),
                    r.GetDecimal(4), r.GetDecimal(5), r.GetDecimal(6), r.GetDecimal(7),
                    r.GetString(8), r.GetDateTime(9));
            }

            var items = new List<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)>();
            await using (var cmd = new NpgsqlCommand(
                @"SELECT si.product_id, p.name, si.quantity, si.unit_price
                  FROM sale_items si JOIN products p ON p.id = si.product_id
                  WHERE si.sale_id = $1", conn))
            {
                cmd.Parameters.AddWithValue(saleId);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    items.Add((r.GetGuid(0), r.GetString(1), r.GetInt32(2), r.GetDecimal(3)));
            }

            return new SaleWithItems(sale, items);
        }

        public async Task<(IEnumerable<SaleWithItems> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);

            var sales = new List<(Guid Id, Sale Sale)>();
            int total = 0;

            await using (var cmd = new NpgsqlCommand(
                @"SELECT id, tenant_id, customer_id, reservation_id,
                         subtotal, tax_rate, tax_amount, total, status, created_at,
                         count(*) OVER() AS total_count
                  FROM sales WHERE tenant_id = $1 ORDER BY created_at DESC LIMIT $2 OFFSET $3", conn))
            {
                cmd.Parameters.AddWithValue(tenantId);
                cmd.Parameters.AddWithValue(pageSize);
                cmd.Parameters.AddWithValue((page - 1) * pageSize);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    total = r.GetInt32(10);
                    var s = Sale.Reconstitute(r.GetGuid(0), r.GetGuid(1),
                        r.IsDBNull(2) ? null : r.GetGuid(2),
                        r.IsDBNull(3) ? null : r.GetGuid(3),
                        r.GetDecimal(4), r.GetDecimal(5), r.GetDecimal(6), r.GetDecimal(7),
                        r.GetString(8), r.GetDateTime(9));
                    sales.Add((r.GetGuid(0), s));
                }
            }

            if (sales.Count == 0) return ([], 0);

            var saleIds = sales.Select(s => s.Id).ToArray();
            var itemsBySale = new Dictionary<Guid, List<(Guid, string, int, decimal)>>();

            await using (var cmd = new NpgsqlCommand(
                @"SELECT si.sale_id, si.product_id, p.name, si.quantity, si.unit_price
                  FROM sale_items si JOIN products p ON p.id = si.product_id
                  WHERE si.sale_id = ANY($1)", conn))
            {
                cmd.Parameters.AddWithValue(saleIds);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var sid = r.GetGuid(0);
                    if (!itemsBySale.ContainsKey(sid)) itemsBySale[sid] = [];
                    itemsBySale[sid].Add((r.GetGuid(1), r.GetString(2), r.GetInt32(3), r.GetDecimal(4)));
                }
            }

            var result = sales.Select(s =>
                new SaleWithItems(s.Sale, itemsBySale.TryGetValue(s.Id, out var i) ? i : []));

            return (result, total);
        }

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Guid?> FindTodayReservationAsync(Guid tenantId, Guid customerId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            // Busca en NexaBook reservations — misma DB, diferente schema lógico
            await using var cmd = new NpgsqlCommand(
                @"SELECT id FROM reservations
                  WHERE tenant_id = $1 AND customer_id = $2
                    AND reservation_date = CURRENT_DATE
                    AND status IN ('pending','confirmed','arrived')
                  ORDER BY time_slot ASC LIMIT 1", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(customerId);
            var result = await cmd.ExecuteScalarAsync();
            return result is Guid g ? g : null;
        }
    }
}
