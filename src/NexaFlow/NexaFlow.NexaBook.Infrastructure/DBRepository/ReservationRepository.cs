using Dapper;
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
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, reservation.TenantId);
            await conn.ExecuteAsync(
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
                });
        }

        public async Task UpdateAsync(Reservation reservation)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, reservation.TenantId);
            await conn.ExecuteAsync(
                @"UPDATE reservations SET status = @Status, reservation_date = @ReservationDate, time_slot = @TimeSlot
                  WHERE id = @Id AND tenant_id = @TenantId",
                new
                {
                    reservation.Id,
                    reservation.TenantId,
                    ReservationDate = reservation.ReservationDate.ToDateTime(TimeOnly.MinValue),
                    TimeSlot = reservation.TimeSlot.ToTimeSpan(),
                    Status = reservation.Status.ToString().ToLowerInvariant()
                });
        }

        public async Task<Reservation?> GetByIdAsync(Guid tenantId, Guid reservationId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM reservations WHERE id = @Id AND tenant_id = @TenantId",
                new { Id = reservationId, TenantId = tenantId });
            return row is null ? null : MapReservation(row);
        }

        public async Task<bool> ExistsConflictAsync(Guid tenantId, DateOnly date, TimeOnly timeSlot, Guid? excludeReservationId = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            return await conn.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM reservations
                    WHERE tenant_id = @TenantId
                      AND reservation_date = @Date
                      AND time_slot = @TimeSlot
                      AND status IN ('pending','confirmed')
                      AND (@ExcludeId IS NULL OR id <> @ExcludeId)
                  )",
                new
                {
                    TenantId = tenantId,
                    Date = date.ToDateTime(TimeOnly.MinValue),
                    TimeSlot = timeSlot.ToTimeSpan(),
                    ExcludeId = excludeReservationId
                });
        }

        public async Task<IEnumerable<Reservation>> GetByDateAsync(Guid tenantId, DateOnly date)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var result = await conn.QueryAsync<dynamic>(
                @"SELECT * FROM reservations
                  WHERE tenant_id = @TId AND reservation_date = @Date
                  ORDER BY time_slot",
                new { TId = tenantId, Date = date.ToDateTime(TimeOnly.MinValue) });
            return result.Select(r => MapReservation(r)).Cast<Reservation>().ToList();
        }

        public async Task<(IEnumerable<ReservationWithCustomer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize, string? status = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var statusFilter = status is not null ? "AND r.status = @Status" : "";
            var result = await conn.QueryAsync<dynamic>(
                $@"SELECT r.*, c.name AS customer_name, count(*) OVER() AS TotalCount
                  FROM reservations r
                  JOIN customers c ON c.id = r.customer_id
                  WHERE r.tenant_id = @TId {statusFilter}
                  ORDER BY r.reservation_date DESC, r.time_slot
                  LIMIT @Limit OFFSET @Offset",
                new { TId = tenantId, Status = status, Limit = pageSize, Offset = (page - 1) * pageSize });

            var list = result.ToList();
            var total = list.Count > 0 ? (int)list[0].totalcount : 0;
            return (list.Select(r => MapWithCustomer(r)).Cast<ReservationWithCustomer>().ToList(), total);
        }

        public async Task<(IEnumerable<ReservationWithCustomer> Items, int Total)> GetByCustomerAsync(Guid tenantId, Guid customerId, int page, int pageSize)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var result = await conn.QueryAsync<dynamic>(
                @"SELECT r.*, c.name AS customer_name, count(*) OVER() AS TotalCount
                  FROM reservations r
                  JOIN customers c ON c.id = r.customer_id
                  WHERE r.tenant_id = @TId AND r.customer_id = @CustomerId
                  ORDER BY r.reservation_date DESC, r.time_slot
                  LIMIT @Limit OFFSET @Offset",
                new { TId = tenantId, CustomerId = customerId, Limit = pageSize, Offset = (page - 1) * pageSize });

            var list = result.ToList();
            var total = list.Count > 0 ? (int)list[0].totalcount : 0;
            return (list.Select(r => MapWithCustomer(r)).Cast<ReservationWithCustomer>().ToList(), total);
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync(Guid tenantId, DateOnly from, DateOnly to)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var result = await conn.QueryAsync<dynamic>(
                @"SELECT status, COUNT(*) AS count FROM reservations
                  WHERE tenant_id = @TId
                    AND reservation_date BETWEEN @From AND @To
                  GROUP BY status",
                new { TId = tenantId, From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MinValue) });
            return result.ToDictionary(r => (string)r.status, r => (int)r.count);
        }

        private static Reservation MapReservation(dynamic r)
        {
            var status = Enum.Parse<ReservationStatus>((string)r.status, ignoreCase: true);
            var reservation = new Reservation(
                (Guid)r.tenant_id,
                (Guid)r.customer_id,
                DateOnly.FromDateTime((DateTime)r.reservation_date),
                TimeOnly.FromTimeSpan((TimeSpan)r.time_slot));

            // Sincroniza el estado real desde la BD
            while (reservation.Status != status)
            {
                if (status == ReservationStatus.Confirmed) { reservation.Confirm(); break; }
                if (status == ReservationStatus.Cancelled) { reservation.Cancel(); break; }
                if (status == ReservationStatus.Arrived) { reservation.Confirm(); reservation.MarkArrived(); break; }
                if (status == ReservationStatus.Completed) { reservation.Confirm(); reservation.MarkArrived(); reservation.Complete(); break; }
                break;
            }
            return reservation;
        }

        private static ReservationWithCustomer MapWithCustomer(dynamic r) =>
            new(MapReservation(r), (string)r.customer_name);

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
        }
    }
}
