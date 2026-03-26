using NexaFlow.NexaBook.Domain.Entities;

namespace NexaFlow.NexaBook.Application.Interfaces.Repositories
{
    public interface IReservationRepository
    {
        Task SaveAsync(Reservation reservation);
        Task UpdateAsync(Reservation reservation);
        Task<Reservation?> GetByIdAsync(Guid tenantId, Guid reservationId);
        Task<bool> ExistsConflictAsync(Guid tenantId, DateOnly date, TimeOnly timeSlot, Guid? excludeReservationId = null);
        Task<IEnumerable<Reservation>> GetByDateAsync(Guid tenantId, DateOnly date);
        Task<(IEnumerable<ReservationWithCustomer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize, string? status = null);
        Task<(IEnumerable<ReservationWithCustomer> Items, int Total)> GetByCustomerAsync(Guid tenantId, Guid customerId, int page, int pageSize);

        /// <summary>
        /// Retorna conteos de reservas agrupados por estado para un rango de fechas.
        /// Usado para el resumen del admin.
        /// </summary>
        Task<Dictionary<string, int>> GetStatusCountsAsync(Guid tenantId, DateOnly from, DateOnly to);
    }
}
