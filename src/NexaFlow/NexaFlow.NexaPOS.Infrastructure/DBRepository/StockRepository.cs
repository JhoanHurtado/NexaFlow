using Dapper;
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
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");

            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM product_stock WHERE product_id = @ProductId AND tenant_id = @TenantId",
                new { ProductId = productId, TenantId = tenantId });

            if (row is null) return null;
            return new ProductStock((Guid)row.product_id, (Guid)row.tenant_id, (int)row.quantity, (int)row.low_stock_threshold);
        }

        public async Task SaveAsync(ProductStock stock)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{stock.TenantId}'");

            await conn.ExecuteAsync(
                @"INSERT INTO product_stock (product_id, tenant_id, quantity, low_stock_threshold)
                  VALUES (@ProductId, @TenantId, @Quantity, @LowStockThreshold)",
                stock);
        }

        public async Task UpdateAsync(ProductStock stock)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{stock.TenantId}'");

            await conn.ExecuteAsync(
                @"UPDATE product_stock SET quantity = @Quantity, updated_at = NOW()
                  WHERE product_id = @ProductId AND tenant_id = @TenantId",
                stock);
        }
    }
}
