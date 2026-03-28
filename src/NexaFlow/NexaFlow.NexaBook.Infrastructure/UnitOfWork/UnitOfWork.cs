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
            await using var setCmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", _conn);
            await setCmd.ExecuteNonQueryAsync();
            _tx = await _conn.BeginTransactionAsync();
        }

        public async Task SaveCustomerAsync(Customer customer)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO customers (id,tenant_id,name,phone,email) VALUES ($1,$2,$3,$4,$5)", _conn, _tx);
            cmd.Parameters.AddWithValue(customer.Id);
            cmd.Parameters.AddWithValue(customer.TenantId);
            cmd.Parameters.AddWithValue(customer.Name);
            cmd.Parameters.AddWithValue((object?)customer.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)customer.Email ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await using var cmd = new NpgsqlCommand(
                "UPDATE customers SET name=$1,phone=$2,email=$3 WHERE id=$4 AND tenant_id=$5", _conn, _tx);
            cmd.Parameters.AddWithValue(customer.Name);
            cmd.Parameters.AddWithValue((object?)customer.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)customer.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue(customer.Id);
            cmd.Parameters.AddWithValue(customer.TenantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveReservationAsync(Reservation reservation)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO reservations (id,tenant_id,customer_id,reservation_date,time_slot,status) VALUES ($1,$2,$3,$4,$5,$6)", _conn, _tx);
            cmd.Parameters.AddWithValue(reservation.Id);
            cmd.Parameters.AddWithValue(reservation.TenantId);
            cmd.Parameters.AddWithValue(reservation.CustomerId);
            cmd.Parameters.AddWithValue(reservation.ReservationDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue(reservation.TimeSlot.ToTimeSpan());
            cmd.Parameters.AddWithValue(reservation.Status.ToString().ToLowerInvariant());
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateReservationAsync(Reservation reservation)
        {
            await using var cmd = new NpgsqlCommand(
                "UPDATE reservations SET status=$1,reservation_date=$2,time_slot=$3 WHERE id=$4 AND tenant_id=$5", _conn, _tx);
            cmd.Parameters.AddWithValue(reservation.Status.ToString().ToLowerInvariant());
            cmd.Parameters.AddWithValue(reservation.ReservationDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue(reservation.TimeSlot.ToTimeSpan());
            cmd.Parameters.AddWithValue(reservation.Id);
            cmd.Parameters.AddWithValue(reservation.TenantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EnqueueEventAsync(DomainEvent domainEvent)
        {
            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO pos_events (tenant_id,event_type,aggregate_id,aggregate_type,payload,published)
                  VALUES ($1,$2,$3,$4,$5::jsonb,FALSE)", _conn, _tx);
            cmd.Parameters.AddWithValue(domainEvent.TenantId);
            cmd.Parameters.AddWithValue(domainEvent.EventType);
            cmd.Parameters.AddWithValue(domainEvent.AggregateId);
            cmd.Parameters.AddWithValue(domainEvent.AggregateType);
            cmd.Parameters.AddWithValue(payload);
            await cmd.ExecuteNonQueryAsync();
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
