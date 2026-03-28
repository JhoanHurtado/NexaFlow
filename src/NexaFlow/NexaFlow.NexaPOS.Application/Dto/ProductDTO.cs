namespace NexaFlow.NexaPOS.Application.Dto
{
    public record ProductDTO(Guid Id, string Name, decimal Price, int Stock, int LowStockThreshold, bool Active);
}
