namespace NexaFlow.NexaBook.Application.Dto
{
    public record ReservationDTO(
        Guid Id,
        Guid TenantId,
        Guid CustomerId,
        string CustomerName,
        DateOnly ReservationDate,
        TimeOnly TimeSlot,
        string Status,
        string? Notes,
        DateTime CreatedAt
    );
}
