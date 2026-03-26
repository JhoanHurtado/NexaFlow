using Dapper;
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
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");

        var row = await conn.QuerySingleAsync<dynamic>(
            @"SELECT
                COUNT(*)::int          AS sale_count,
                COALESCE(SUM(total), 0) AS total,
                COALESCE(AVG(total), 0) AS average
              FROM sales
              WHERE tenant_id = @TId
                AND created_at::date BETWEEN @From AND @To",
            new { TId = tenantId, From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MaxValue) });

        return new AverageTicket(tenantId, (decimal)row.average, (decimal)row.total,
            (int)row.sale_count, from, to);
    }

    public async Task<IEnumerable<DailySalesSummary>> GetDailySummaryAsync(Guid tenantId, DateOnly from, DateOnly to)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT
                created_at::date                AS sale_date,
                COUNT(*)::int                   AS sale_count,
                COALESCE(SUM(total), 0)         AS total_revenue,
                COALESCE(AVG(total), 0)         AS average_ticket
              FROM sales
              WHERE tenant_id = @TId
                AND created_at::date BETWEEN @From AND @To
              GROUP BY created_at::date
              ORDER BY created_at::date",
            new { TId = tenantId, From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MaxValue) });

        return rows.Select(r => new DailySalesSummary(
            tenantId,
            DateOnly.FromDateTime((DateTime)r.sale_date),
            (decimal)r.total_revenue,
            (int)r.sale_count,
            (decimal)r.average_ticket));
    }
}
