namespace NexaFlow.NexaBook.Application.Records.Create
{
    public record CreateReservationRequest(
        Guid CustomerId,
        DateOnly ReservationDate,
        TimeOnly TimeSlot,
        string? Notes
    );
}
