namespace NexaFlow.NexaBook.Application.Dto
{
    /// <summary>
    /// Resumen de reservas para un rango de fechas.
    /// Usado en el panel del admin para ver métricas generales.
    /// </summary>
    public record ReservationSummaryDTO(
        DateOnly From,
        DateOnly To,
        int Total,
        int Pending,
        int Confirmed,
        int Arrived,
        int Completed,
        int Cancelled
    );
}
