using NexaFlow.NexaBook.Domain.Events;

namespace NexaFlow.NexaBook.Application.Interfaces.UnitOfWork
{
    /// <summary>
    /// Unidad de trabajo que agrupa múltiples operaciones en una sola transacción DB.
    /// Implementa el Outbox Pattern: la entidad y sus eventos se persisten atómicamente.
    /// <para>
    /// Ciclo de vida:
    /// <code>
    /// await uow.BeginAsync(tenantId);
    /// try {
    ///     await uow.SaveReservationAsync(...);
    ///     await uow.EnqueueEventAsync(...);
    ///     await uow.CommitAsync();
    /// } catch {
    ///     await uow.RollbackAsync();
    ///     throw;
    /// }
    /// </code>
    /// </para>
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable, IDisposable
    {
        /// <summary>Abre la conexión, aplica RLS del tenant e inicia la transacción.</summary>
        Task BeginAsync(Guid tenantId);

        /// <summary>Confirma todos los cambios de la transacción.</summary>
        Task CommitAsync();

        /// <summary>Revierte todos los cambios de la transacción.</summary>
        Task RollbackAsync();

        /// <summary>Inserta un cliente dentro de la transacción activa.</summary>
        Task SaveCustomerAsync(Domain.Entities.Customer customer);

        /// <summary>Actualiza un cliente dentro de la transacción activa.</summary>
        Task UpdateCustomerAsync(Domain.Entities.Customer customer);

        /// <summary>Inserta una reserva dentro de la transacción activa.</summary>
        Task SaveReservationAsync(Domain.Entities.Reservation reservation);

        /// <summary>Actualiza el estado de una reserva dentro de la transacción activa.</summary>
        Task UpdateReservationAsync(Domain.Entities.Reservation reservation);

        /// <summary>
        /// Inserta un evento de dominio en <c>pos_events</c> con <c>published = FALSE</c>
        /// dentro de la transacción activa.
        /// </summary>
        Task EnqueueEventAsync(DomainEvent domainEvent);
    }
}
