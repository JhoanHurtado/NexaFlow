using System.Text.Json.Serialization;

namespace NexaFlow.NexaPOS.Application.Records.Update
{
    public record UpdateCustomerRequest(
        [property: JsonPropertyName("name")]  string Name,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("email")] string? Email);
}
