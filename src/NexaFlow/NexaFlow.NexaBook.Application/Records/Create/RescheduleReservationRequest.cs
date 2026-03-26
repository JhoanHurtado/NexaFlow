namespace NexaFlow.NexaBook.Application.Records.Create
{
    public record RescheduleReservationRequest(DateOnly NewDate, TimeOnly NewTimeSlot);
}
