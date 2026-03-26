namespace NexaFlow.NexaPOS.Application.Dto
{
    public record SaleItemDTO(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);

    public record SaleDTO(Guid Id, Guid TenantId, Guid? CustomerId, Guid? ReservationId, decimal Total, DateTime CreatedAt, IEnumerable<SaleItemDTO> Items);
}
