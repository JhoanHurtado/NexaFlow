namespace NexaFlow.NexaBook.Application.Dto
{
    /// <summary>
    /// Vista de agenda diaria para el admin/tenant.
    /// Muestra todas las reservas del día con estadísticas de estado.
    /// </summary>
    public record AgendaDTO(
        DateOnly Date,
        int TotalReservations,
        int Pending,
        int Confirmed,
        int Arrived,
        int Completed,
        int Cancelled,
        IEnumerable<ReservationDTO> Reservations
    );
}
