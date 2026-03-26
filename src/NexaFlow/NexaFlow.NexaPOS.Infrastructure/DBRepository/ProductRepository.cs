using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Domain.Entities;
using Npgsql;
using Dapper;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        public ProductRepository(string connectionString) => _connectionString = connectionString;

        public async Task SaveAsync(Product product)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
            INSERT INTO products (id, tenant_id, name, price, active)
            VALUES (@Id, @TenantId, @Name, @Price, @IsActive)";
            await connection.ExecuteAsync(sql, product);
        }

        public async Task<Product?> GetByIdAsync(Guid tenantId, Guid productId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM products WHERE id = @Id AND tenant_id = @TenantId",
                new { Id = productId, TenantId = tenantId });
            if (row is null) return null;
            return new Product((Guid)row.tenant_id, (string)row.name, (decimal)row.price);
        }

        public async Task<(IEnumerable<Product> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var result = await conn.QueryAsync<dynamic>(
                @"SELECT *, count(*) OVER() as TotalCount FROM products
                  WHERE tenant_id = @TId AND active = TRUE
                  ORDER BY name LIMIT @Limit OFFSET @Offset",
                new { TId = tenantId, Limit = pageSize, Offset = (page - 1) * pageSize });

            var items = result.Select(r => new Product((Guid)r.tenant_id, (string)r.name, (decimal)r.price));
            var total = result.FirstOrDefault()?.totalcount ?? 0;
            return (items, (int)total);
        }

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
        }
    }
}
