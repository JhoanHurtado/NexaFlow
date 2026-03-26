namespace NexaFlow.NexaBook.Application.Dto
{
    /// <summary>
    /// Disponibilidad de horarios para una fecha.
    /// Usado por el cliente al momento de crear una reserva.
    /// </summary>
    public record AvailabilityDTO(DateOnly Date, IEnumerable<TimeSlotDTO> Slots);

    /// <summary>Un slot de tiempo con su estado de disponibilidad.</summary>
    public record TimeSlotDTO(TimeOnly Time, bool Available);
}
