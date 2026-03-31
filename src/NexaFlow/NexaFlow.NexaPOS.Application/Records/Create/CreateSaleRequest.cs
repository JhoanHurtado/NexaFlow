using System.Text.Json.Serialization;

namespace NexaFlow.NexaPOS.Application.Records.Create
{
    public record CreateSaleItemRequest(
        [property: JsonPropertyName("productId")] Guid ProductId,
        [property: JsonPropertyName("quantity")]  int Quantity);

    public record CreateSaleRequest(
        [property: JsonPropertyName("customerId")]    Guid? CustomerId,
        [property: JsonPropertyName("reservationId")] Guid? ReservationId,
        [property: JsonPropertyName("items")]         IEnumerable<CreateSaleItemRequest> Items);

    public record CreateCustomerRequest(
        [property: JsonPropertyName("name")]  string Name,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("email")] string? Email);
}
