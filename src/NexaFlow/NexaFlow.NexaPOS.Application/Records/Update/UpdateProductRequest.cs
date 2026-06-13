using System.Text.Json.Serialization;

namespace NexaFlow.NexaPOS.Application.Records.Update
{
    public record UpdateProductRequest(
        [property: JsonPropertyName("name")]              string? Name,
        [property: JsonPropertyName("price")]             decimal? Price,
        [property: JsonPropertyName("stock")]             int? Stock,
        [property: JsonPropertyName("lowStockThreshold")] int? LowStockThreshold,
        [property: JsonPropertyName("active")]            bool? Active);
}
