using Dapper;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Domain.Events;
using Npgsql;
using System.Text.Json;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository.Events
{
    /// <summary>
    /// Persiste eventos de dominio en la tabla <c>pos_events</c> fuera de una transacción atómica.
    /// Usado para eventos que no necesitan coordinarse con otras escrituras.
    /// Para eventos dentro de una venta o creación de producto, usar <c>IUnitOfWork.EnqueueEventAsync</c>.
    /// El payload se serializa como JSONB usando el tipo concreto del evento para preservar todos los campos.
    /// </summary>
    public class EventRepository : IEventRepository
    {
        private readonly string _connectionString;

        /// <param name="connectionString">Cadena de conexión PostgreSQL.</param>
        public EventRepository(string connectionString) => _connectionString = connectionString;

        /// <inheritdoc/>
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
