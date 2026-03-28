using System.Text.Json.Serialization;

namespace NexaFlow.NexaPOS.Application.Records.Create
{
    public record CreateProductRequest(
        [property: JsonPropertyName("name")]               string Name,
        [property: JsonPropertyName("price")]              decimal Price,
        [property: JsonPropertyName("initialStock")]       int InitialStock = 0,
        [property: JsonPropertyName("lowStockThreshold")]  int LowStockThreshold = 5);
}
