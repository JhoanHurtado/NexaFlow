namespace NexaFlow.NexaPOS.Application.Records.Create
{
    public record CreateSaleItemRequest(Guid ProductId, int Quantity);

    public record CreateSaleRequest(
        Guid? CustomerId,
        Guid? ReservationId,
        IEnumerable<CreateSaleItemRequest> Items
    );

    public record CreateCustomerRequest(string Name, string? Phone, string? Email);
}
