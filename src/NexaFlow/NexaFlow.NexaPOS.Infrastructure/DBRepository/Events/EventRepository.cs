using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Domain.Events;
using Npgsql;
using System.Text.Json;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository.Events
{
    public class EventRepository : IEventRepository
    {
        private readonly string _connectionString;
        public EventRepository(string connectionString) => _connectionString = connectionString;

        public async Task PublishAsync(DomainEvent domainEvent)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using (var setCmd = new NpgsqlCommand($"SET app.tenant_id = '{domainEvent.TenantId}'", conn))
                await setCmd.ExecuteNonQueryAsync();

            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO pos_events (tenant_id, event_type, aggregate_id, aggregate_type, payload)
                  VALUES ($1, $2, $3, $4, $5::jsonb)", conn);
            cmd.Parameters.AddWithValue(domainEvent.TenantId);
            cmd.Parameters.AddWithValue(domainEvent.EventType);
            cmd.Parameters.AddWithValue(domainEvent.AggregateId);
            cmd.Parameters.AddWithValue(domainEvent.AggregateType);
            cmd.Parameters.AddWithValue(payload);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
