using Dapper;
using NexaFlow.NexaBook.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaBook.Domain.Entities;
using NexaFlow.NexaBook.Domain.Events;
using Npgsql;
using System.Text.Json;

namespace NexaFlow.NexaBook.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _connectionString;
        private NpgsqlConnection? _conn;
        private NpgsqlTransaction? _tx;

        public UnitOfWork(string connectionString) => _connectionString = connectionString;

        public async Task BeginAsync(Guid tenantId)
        {
            _conn = new NpgsqlConnection(_connectionString);
            await _conn.OpenAsync();
            await _conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
            _tx = await _conn.BeginTransactionAsync();
        }

        public async Task SaveCustomerAsync(Customer customer) =>
            await _conn!.ExecuteAsync(
                "INSERT INTO customers (id, tenant_id, name, phone, email) VALUES (@Id, @TenantId, @Name, @Phone, @Email)",
                customer, _tx);

        public async Task UpdateCustomerAsync(Customer customer) =>
            await _conn!.ExecuteAsync(
                "UPDATE customers SET name = @Name, phone = @Phone, email = @Email WHERE id = @Id AND tenant_id = @TenantId",
                customer, _tx);

        public async Task SaveReservationAsync(Reservation reservation) =>
            await _conn!.ExecuteAsync(
                @"INSERT INTO reservations (id, tenant_id, customer_id, reservation_date, time_slot, status)
                  VALUES (@Id, @TenantId, @CustomerId, @ReservationDate, @TimeSlot, @Status)",
                new
                {
                    reservation.Id,
                    reservation.TenantId,
                    reservation.CustomerId,
                    ReservationDate = reservation.ReservationDate.ToDateTime(TimeOnly.MinValue),
                    TimeSlot = reservation.TimeSlot.ToTimeSpan(),
                    Status = reservation.Status.ToString().ToLowerInvariant()
                }, _tx);

        public async Task UpdateReservationAsync(Reservation reservation) =>
            await _conn!.ExecuteAsync(
                @"UPDATE reservations SET status = @Status, reservation_date = @ReservationDate, time_slot = @TimeSlot
                  WHERE id = @Id AND tenant_id = @TenantId",
                new
                {
                    reservation.Id,
                    reservation.TenantId,
                    ReservationDate = reservation.ReservationDate.ToDateTime(TimeOnly.MinValue),
                    TimeSlot = reservation.TimeSlot.ToTimeSpan(),
                    Status = reservation.Status.ToString().ToLowerInvariant()
                }, _tx);

        public async Task EnqueueEventAsync(DomainEvent domainEvent)
        {
            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            await _conn!.ExecuteAsync(
                @"INSERT INTO pos_events (tenant_id, event_type, aggregate_id, aggregate_type, payload, published)
                  VALUES (@TenantId, @EventType, @AggregateId, @AggregateType, @Payload::jsonb, FALSE)",
                new
                {
                    domainEvent.TenantId,
                    domainEvent.EventType,
                    domainEvent.AggregateId,
                    domainEvent.AggregateType,
                    Payload = payload
                }, _tx);
        }

        public async Task CommitAsync() => await _tx!.CommitAsync();
        public async Task RollbackAsync() => await _tx!.RollbackAsync();

        public async ValueTask DisposeAsync()
        {
            if (_tx is not null) await _tx.DisposeAsync();
            if (_conn is not null) await _conn.DisposeAsync();
        }

        public void Dispose()
        {
            _tx?.Dispose();
            _conn?.Dispose();
        }
    }
}
