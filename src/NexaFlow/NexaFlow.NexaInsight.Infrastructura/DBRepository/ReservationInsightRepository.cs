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
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, tenantId);
        await using var cmd = new NpgsqlCommand(
            @"SELECT
                COUNT(*)::int,
                COUNT(*) FILTER (WHERE status = 'cancelled')::int,
                ROUND(COUNT(*) FILTER (WHERE status = 'cancelled') * 100.0 / NULLIF(COUNT(*),0), 2)
              FROM reservations
              WHERE tenant_id = $1 AND reservation_date BETWEEN $2 AND $3", conn);
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.AddWithValue(from);
        cmd.Parameters.AddWithValue(to);
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return new CancellationRate(tenantId, r.GetInt32(0), r.GetInt32(1),
            r.IsDBNull(2) ? 0m : r.GetDecimal(2), from, to);
    }

    private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
    {
        await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
