namespace NexaFlow.NexaPOS.Application.Records.Create
{
    public record CreateProductRequest(string Name, decimal Price, int InitialStock = 0, int LowStockThreshold = 5);
}
