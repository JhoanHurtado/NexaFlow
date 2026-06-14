using System.Text.Json.Serialization;

namespace NexaFlow.NexaPOS.Application.Dto
{
    public record UpdatedResponse(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("id")]      Guid   Id);

    public record SaleStatusUpdatedResponse(
        [property: JsonPropertyName("updated")] bool   Updated,
        [property: JsonPropertyName("saleId")]  string SaleId,
        [property: JsonPropertyName("status")]  string Status);

    public record SeedResponse(
        [property: JsonPropertyName("message")]      string Message,
        [property: JsonPropertyName("products")]     int    Products,
        [property: JsonPropertyName("customers")]    int    Customers,
        [property: JsonPropertyName("sales")]        int    Sales,
        [property: JsonPropertyName("reservations")] int    Reservations);
}
