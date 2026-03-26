using Dapper;
using NexaFlow.NexaBook.Application.Interfaces.Events;
using NexaFlow.NexaBook.Domain.Events;
using Npgsql;
using System.Text.Json;

namespace NexaFlow.NexaBook.Infrastructure.DBRepository.Events
{
    public class EventRepository : IEventRepository
    {
        private readonly string _connectionString;
        public EventRepository(string connectionString) => _connectionString = connectionString;

        public async Task PublishAsync(DomainEvent domainEvent)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{domainEvent.TenantId}'");

            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            await conn.ExecuteAsync(
                @"INSERT INTO pos_events (tenant_id, event_type, aggregate_id, aggregate_type, payload)
                  VALUES (@TenantId, @EventType, @AggregateId, @AggregateType, @Payload::jsonb)",
                new
                {
                    domainEvent.TenantId,
                    domainEvent.EventType,
                    domainEvent.AggregateId,
                    domainEvent.AggregateType,
                    Payload = payload
                });
        }
    }
}
