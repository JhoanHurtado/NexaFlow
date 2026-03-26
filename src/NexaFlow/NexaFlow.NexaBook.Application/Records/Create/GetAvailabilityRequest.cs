namespace NexaFlow.NexaBook.Application.Records.Create
{
    /// <summary>
    /// Request para consultar disponibilidad de horarios en una fecha.
    /// <para><c>SlotDurationMinutes</c>: duración de cada turno en minutos (default 60).</para>
    /// <para><c>OpenTime</c>: hora de apertura del negocio (default 08:00).</para>
    /// <para><c>CloseTime</c>: hora de cierre del negocio (default 20:00).</para>
    /// </summary>
    public record GetAvailabilityRequest(
        DateOnly Date,
        int SlotDurationMinutes = 60,
        TimeOnly? OpenTime = null,
        TimeOnly? CloseTime = null
    );
}
