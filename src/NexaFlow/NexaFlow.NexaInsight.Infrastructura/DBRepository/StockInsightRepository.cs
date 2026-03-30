using NexaFlow.NexaInsight.Application.Interfaces.Repositories;
using NexaFlow.NexaInsight.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaInsight.Infrastructura.DBRepository;

public class StockInsightRepository : IStockInsightRepository
{
    private readonly string _conn;
    public StockInsightRepository(string conn) => _conn = conn;

    /// <summary>
    /// Retorna productos con stock igual o por debajo del umbral mínimo.
    /// Consulta product_stock JOIN products — ambas tablas existen en el schema.
    /// </summary>
    public async Task<IEnumerable<LowStockProduct>> GetLowStockProductsAsync(Guid tenantId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, tenantId);
        await using var cmd = new NpgsqlCommand(
            @"SELECT
                ps.product_id,
                p.name,
                ps.quantity,
                ps.low_stock_threshold
              FROM product_stock ps
              JOIN products p ON p.id = ps.product_id
              WHERE ps.tenant_id = $1
                AND p.active = TRUE
                AND ps.quantity <= ps.low_stock_threshold
              ORDER BY ps.quantity ASC", conn);
        cmd.Parameters.AddWithValue(tenantId);
        await using var r = await cmd.ExecuteReaderAsync();
        var result = new List<LowStockProduct>();
        while (await r.ReadAsync())
            result.Add(new LowStockProduct(
                tenantId,
                r.GetGuid(0),
                r.GetString(1),
                r.GetInt32(2),
                r.GetInt32(3),
                r.GetInt32(2) == 0));
        return result;
    }

    private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
    {
        await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
