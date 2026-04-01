using System.Text.Json.Serialization;

namespace NexaFlow.NexaPOS.Application.Records.Update;

public record UpdateSaleStatusRequest(
    [property: JsonPropertyName("status")] string Status);
