using NexaFlow.NexaInsight.Application.Interfaces.Repositories;
using NexaFlow.NexaInsight.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaInsight.Infrastructura.DBRepository;

public class SalesInsightRepository : ISalesInsightRepository
{
    private readonly string _conn;
    public SalesInsightRepository(string conn) => _conn = conn;

    public async Task<AverageTicket> GetAverageTicketAsync(Guid tenantId, DateOnly from, DateOnly to)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, tenantId);
        await using var cmd = new NpgsqlCommand(
            @"SELECT COUNT(*)::int, COALESCE(SUM(total),0), COALESCE(AVG(total),0)
              FROM sales
              WHERE tenant_id = $1 AND created_at::date BETWEEN $2 AND $3", conn);
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.AddWithValue(from.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue(to.ToDateTime(TimeOnly.MaxValue));
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return new AverageTicket(tenantId, r.GetDecimal(2), r.GetDecimal(1), r.GetInt32(0), from, to);
    }

    public async Task<IEnumerable<DailySalesSummary>> GetDailySummaryAsync(Guid tenantId, DateOnly from, DateOnly to)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, tenantId);
        await using var cmd = new NpgsqlCommand(
            @"SELECT created_at::date, COUNT(*)::int, COALESCE(SUM(total),0), COALESCE(AVG(total),0)
              FROM sales
              WHERE tenant_id = $1 AND created_at::date BETWEEN $2 AND $3
              GROUP BY created_at::date ORDER BY created_at::date", conn);
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.AddWithValue(from.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue(to.ToDateTime(TimeOnly.MaxValue));
        await using var r = await cmd.ExecuteReaderAsync();
        var result = new List<DailySalesSummary>();
        while (await r.ReadAsync())
            result.Add(new DailySalesSummary(tenantId,
                DateOnly.FromDateTime(r.GetDateTime(0)),
                r.GetDecimal(2), r.GetInt32(1), r.GetDecimal(3)));
        return result;
    }

    private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
    {
        await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
