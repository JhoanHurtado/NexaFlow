using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        public ProductRepository(string connectionString) => _connectionString = connectionString;

        public async Task SaveAsync(Product product)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO products (id, tenant_id, name, price, active) VALUES ($1, $2, $3, $4, $5)", conn);
            cmd.Parameters.AddWithValue(product.Id);
            cmd.Parameters.AddWithValue(product.TenantId);
            cmd.Parameters.AddWithValue(product.Name);
            cmd.Parameters.AddWithValue(product.Price);
            cmd.Parameters.AddWithValue(product.IsActive);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid tenantId, Guid productId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT id, tenant_id, name, price, active FROM products WHERE id = $1 AND tenant_id = $2", conn);
            cmd.Parameters.AddWithValue(productId);
            cmd.Parameters.AddWithValue(tenantId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return Product.Reconstitute(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetDecimal(3), reader.GetBoolean(4));
        }

        public async Task UpdateAsync(Product product)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, product.TenantId);
            await using var cmd = new NpgsqlCommand(
                "UPDATE products SET name = $1, price = $2, active = $3 WHERE id = $4 AND tenant_id = $5", conn);
            cmd.Parameters.AddWithValue(product.Name);
            cmd.Parameters.AddWithValue(product.Price);
            cmd.Parameters.AddWithValue(product.IsActive);
            cmd.Parameters.AddWithValue(product.Id);
            cmd.Parameters.AddWithValue(product.TenantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsByNameAsync(Guid tenantId, string name)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM products WHERE tenant_id=$1 AND LOWER(name)=LOWER($2) AND active=TRUE)", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(name);
            return (bool)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<(IEnumerable<(Product Product, int Stock, int LowStockThreshold)> Items, int Total)> GetPagedAsync(
            Guid tenantId, int page, int pageSize)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                @"SELECT p.id, p.tenant_id, p.name, p.price, p.active,
                         COALESCE(s.quantity, 0)              AS stock,
                         COALESCE(s.low_stock_threshold, 5)   AS low_stock_threshold,
                         count(*) OVER()                      AS total_count
                  FROM products p
                  LEFT JOIN product_stock s ON s.product_id = p.id AND s.tenant_id = p.tenant_id
                  WHERE p.tenant_id = $1
                  ORDER BY p.name LIMIT $2 OFFSET $3", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(pageSize);
            cmd.Parameters.AddWithValue((page - 1) * pageSize);

            var items = new List<(Product, int, int)>();
            int total = 0;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                total = reader.GetInt32(7);
                var product = Product.Reconstitute(
                    reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetDecimal(3), reader.GetBoolean(4));
                items.Add((product, reader.GetInt32(5), reader.GetInt32(6)));
            }
            return (items, total);
        }

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
