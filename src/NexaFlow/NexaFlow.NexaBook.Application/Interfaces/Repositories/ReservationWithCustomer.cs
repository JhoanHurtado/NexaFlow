using NexaFlow.NexaBook.Domain.Entities;

namespace NexaFlow.NexaBook.Application.Interfaces.Repositories
{
    /// <summary>
    /// Proyección que combina una reserva con el nombre del cliente.
    /// </summary>
    public record ReservationWithCustomer(Reservation Reservation, string CustomerName);
}
