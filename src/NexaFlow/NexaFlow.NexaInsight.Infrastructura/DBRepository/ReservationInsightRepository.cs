using Dapper;
using NexaFlow.NexaInsight.Application.Interfaces.Repositories;
using NexaFlow.NexaInsight.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaInsight.Infrastructura.DBRepository;

public class ReservationInsightRepository : IReservationInsightRepository
{
    private readonly string _conn;
    public ReservationInsightRepository(string conn) => _conn = conn;

    public async Task<CancellationRate> GetCancellationRateAsync(Guid tenantId, DateOnly from, DateOnly to)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");

        var row = await conn.QuerySingleAsync<dynamic>(
            @"SELECT
                COUNT(*)::int                                                    AS total,
                COUNT(*) FILTER (WHERE status = 'cancelled')::int               AS cancelled,
                ROUND(
                    COUNT(*) FILTER (WHERE status = 'cancelled') * 100.0
                    / NULLIF(COUNT(*), 0), 2
                )                                                                AS rate
              FROM reservations
              WHERE tenant_id = @TId
                AND reservation_date BETWEEN @From AND @To",
            new { TId = tenantId, From = from, To = to });

        return new CancellationRate(tenantId, (int)row.total, (int)row.cancelled,
            (decimal)(row.rate ?? 0m), from, to);
    }
}
