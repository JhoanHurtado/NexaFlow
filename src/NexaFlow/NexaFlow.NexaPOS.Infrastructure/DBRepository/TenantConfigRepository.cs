using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository
{
    public class TenantConfigRepository : ITenantConfigRepository
    {
        private readonly string _conn;
        public TenantConfigRepository(string conn) => _conn = conn;

        public async Task<TenantConfig> GetOrDefaultAsync(Guid tenantId)
        {
            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                @"SELECT tenant_id, tax_rate, currency, slot_duration_minutes,
                         open_time, close_time, updated_at
                  FROM tenant_config WHERE tenant_id = $1", conn);
            cmd.Parameters.AddWithValue(tenantId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return TenantConfig.Default(tenantId);
            return TenantConfig.Reconstitute(
                r.GetGuid(0),
                r.GetDecimal(1),
                r.GetString(2),
                r.GetInt32(3),
                TimeOnly.FromTimeSpan(r.GetTimeSpan(4)),
                TimeOnly.FromTimeSpan(r.GetTimeSpan(5)),
                r.GetDateTime(6));
        }

        public async Task UpsertAsync(TenantConfig config)
        {
            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO tenant_config
                    (tenant_id, tax_rate, currency, slot_duration_minutes, open_time, close_time, updated_at)
                  VALUES ($1, $2, $3, $4, $5, $6, NOW())
                  ON CONFLICT (tenant_id) DO UPDATE SET
                    tax_rate              = EXCLUDED.tax_rate,
                    currency              = EXCLUDED.currency,
                    slot_duration_minutes = EXCLUDED.slot_duration_minutes,
                    open_time             = EXCLUDED.open_time,
                    close_time            = EXCLUDED.close_time,
                    updated_at            = NOW()", conn);
            cmd.Parameters.AddWithValue(config.TenantId);
            cmd.Parameters.AddWithValue(config.TaxRate);
            cmd.Parameters.AddWithValue(config.Currency);
            cmd.Parameters.AddWithValue(config.SlotDurationMinutes);
            cmd.Parameters.AddWithValue(config.OpenTime.ToTimeSpan());
            cmd.Parameters.AddWithValue(config.CloseTime.ToTimeSpan());
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
