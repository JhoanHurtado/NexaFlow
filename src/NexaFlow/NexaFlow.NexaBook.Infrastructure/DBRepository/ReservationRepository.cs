using NexaFlow.NexaBook.Application.Interfaces.Repositories;
using NexaFlow.NexaBook.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaBook.Infrastructure.DBRepository
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly string _connectionString;
        public ReservationRepository(string connectionString) => _connectionString = connectionString;

        public async Task SaveAsync(Reservation reservation)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, reservation.TenantId);
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO reservations (id,tenant_id,customer_id,reservation_date,time_slot,status) VALUES ($1,$2,$3,$4,$5,$6)", conn);
            cmd.Parameters.AddWithValue(reservation.Id);
            cmd.Parameters.AddWithValue(reservation.TenantId);
            cmd.Parameters.AddWithValue(reservation.CustomerId);
            cmd.Parameters.AddWithValue(reservation.ReservationDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue(reservation.TimeSlot.ToTimeSpan());
            cmd.Parameters.AddWithValue(reservation.Status.ToString().ToLowerInvariant());
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Reservation reservation)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, reservation.TenantId);
            await using var cmd = new NpgsqlCommand(
                "UPDATE reservations SET status=$1,reservation_date=$2,time_slot=$3 WHERE id=$4 AND tenant_id=$5", conn);
            cmd.Parameters.AddWithValue(reservation.Status.ToString().ToLowerInvariant());
            cmd.Parameters.AddWithValue(reservation.ReservationDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue(reservation.TimeSlot.ToTimeSpan());
            cmd.Parameters.AddWithValue(reservation.Id);
            cmd.Parameters.AddWithValue(reservation.TenantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Reservation?> GetByIdAsync(Guid tenantId, Guid reservationId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT id,tenant_id,customer_id,reservation_date,time_slot,status FROM reservations WHERE id=$1 AND tenant_id=$2", conn);
            cmd.Parameters.AddWithValue(reservationId);
            cmd.Parameters.AddWithValue(tenantId);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? MapReservation(r) : null;
        }

        public async Task<bool> ExistsConflictAsync(Guid tenantId, DateOnly date, TimeOnly timeSlot, Guid? excludeId = null)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                @"SELECT EXISTS(SELECT 1 FROM reservations
                  WHERE tenant_id=$1 AND reservation_date=$2 AND time_slot=$3
                    AND status IN ('pending','confirmed')
                    AND ($4::uuid IS NULL OR id<>$4))", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(date.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue(timeSlot.ToTimeSpan());
            cmd.Parameters.AddWithValue((object?)excludeId ?? DBNull.Value);
            return (bool)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<IEnumerable<Reservation>> GetByDateAsync(Guid tenantId, DateOnly date)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT id,tenant_id,customer_id,reservation_date,time_slot,status FROM reservations WHERE tenant_id=$1 AND reservation_date=$2 ORDER BY time_slot", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(date.ToDateTime(TimeOnly.MinValue));
            var items = new List<Reservation>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) items.Add(MapReservation(r));
            return items;
        }

        public async Task<(IEnumerable<ReservationWithCustomer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize, string? status = null)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            var sql = status is null
                ? @"SELECT r.id,r.tenant_id,r.customer_id,r.reservation_date,r.time_slot,r.status,c.name,count(*) OVER() AS total
                    FROM reservations r JOIN customers c ON c.id=r.customer_id
                    WHERE r.tenant_id=$1 ORDER BY r.reservation_date DESC,r.time_slot LIMIT $2 OFFSET $3"
                : @"SELECT r.id,r.tenant_id,r.customer_id,r.reservation_date,r.time_slot,r.status,c.name,count(*) OVER() AS total
                    FROM reservations r JOIN customers c ON c.id=r.customer_id
                    WHERE r.tenant_id=$1 AND r.status=$4 ORDER BY r.reservation_date DESC,r.time_slot LIMIT $2 OFFSET $3";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(pageSize);
            cmd.Parameters.AddWithValue((page - 1) * pageSize);
            if (status is not null) cmd.Parameters.AddWithValue(status);
            var items = new List<ReservationWithCustomer>();
            int total = 0;
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                total = (int)r.GetInt64(7);
                items.Add(new ReservationWithCustomer(MapReservation(r), r.GetString(6)));
            }
            return (items, total);
        }

        public async Task<(IEnumerable<ReservationWithCustomer> Items, int Total)> GetByCustomerAsync(Guid tenantId, Guid customerId, int page, int pageSize)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                @"SELECT r.id,r.tenant_id,r.customer_id,r.reservation_date,r.time_slot,r.status,c.name,count(*) OVER() AS total
                  FROM reservations r JOIN customers c ON c.id=r.customer_id
                  WHERE r.tenant_id=$1 AND r.customer_id=$2
                  ORDER BY r.reservation_date DESC,r.time_slot LIMIT $3 OFFSET $4", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(customerId);
            cmd.Parameters.AddWithValue(pageSize);
            cmd.Parameters.AddWithValue((page - 1) * pageSize);
            var items = new List<ReservationWithCustomer>();
            int total = 0;
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                total = (int)r.GetInt64(7);
                items.Add(new ReservationWithCustomer(MapReservation(r), r.GetString(6)));
            }
            return (items, total);
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync(Guid tenantId, DateOnly from, DateOnly to)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT status,COUNT(*)::int FROM reservations WHERE tenant_id=$1 AND reservation_date BETWEEN $2 AND $3 GROUP BY status", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(from.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue(to.ToDateTime(TimeOnly.MinValue));
            var result = new Dictionary<string, int>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) result[r.GetString(0)] = r.GetInt32(1);
            return result;
        }

        private static Reservation MapReservation(NpgsqlDataReader r)
        {
            var status = Enum.Parse<ReservationStatus>(r.GetString(5), ignoreCase: true);
            return new Reservation(
                r.GetGuid(0),
                r.GetGuid(1),
                r.GetGuid(2),
                DateOnly.FromDateTime(r.GetDateTime(3)),
                TimeOnly.FromTimeSpan(r.GetTimeSpan(4)),
                status,
                DateTime.UtcNow);
        }

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
