using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository
{
    public class StockRepository : IStockRepository
    {
        private readonly string _connectionString;
        public StockRepository(string connectionString) => _connectionString = connectionString;

        public async Task<ProductStock?> GetByProductIdAsync(Guid tenantId, Guid productId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT product_id, tenant_id, quantity, low_stock_threshold FROM product_stock WHERE product_id = $1 AND tenant_id = $2", conn);
            cmd.Parameters.AddWithValue(productId);
            cmd.Parameters.AddWithValue(tenantId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            return new ProductStock(r.GetGuid(0), r.GetGuid(1), r.GetInt32(2), r.GetInt32(3));
        }

        public async Task SaveAsync(ProductStock stock)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, stock.TenantId);
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO product_stock (product_id, tenant_id, quantity, low_stock_threshold) VALUES ($1, $2, $3, $4)", conn);
            cmd.Parameters.AddWithValue(stock.ProductId);
            cmd.Parameters.AddWithValue(stock.TenantId);
            cmd.Parameters.AddWithValue(stock.Quantity);
            cmd.Parameters.AddWithValue(stock.LowStockThreshold);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(ProductStock stock)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, stock.TenantId);
            await using var cmd = new NpgsqlCommand(
                "UPDATE product_stock SET quantity = $1, updated_at = NOW() WHERE product_id = $2 AND tenant_id = $3", conn);
            cmd.Parameters.AddWithValue(stock.Quantity);
            cmd.Parameters.AddWithValue(stock.ProductId);
            cmd.Parameters.AddWithValue(stock.TenantId);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
