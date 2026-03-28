using Amazon.Lambda.APIGatewayEvents;
using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Records.Create;
using System.Text.Json.Serialization;

namespace NexaFlow.NexaBook
{
    [JsonSerializable(typeof(APIGatewayProxyRequest))]
    [JsonSerializable(typeof(APIGatewayProxyResponse))]
    [JsonSerializable(typeof(CreateCustomerRequest))]
    [JsonSerializable(typeof(UpdateCustomerRequest))]
    [JsonSerializable(typeof(CreateReservationRequest))]
    [JsonSerializable(typeof(RescheduleReservationRequest))]
    [JsonSerializable(typeof(CancelReservationRequest))]
    [JsonSerializable(typeof(CustomerDTO))]
    [JsonSerializable(typeof(ReservationDTO))]
    [JsonSerializable(typeof(AgendaDTO))]
    [JsonSerializable(typeof(AvailabilityDTO))]
    [JsonSerializable(typeof(TimeSlotDTO))]
    [JsonSerializable(typeof(ReservationSummaryDTO))]
    [JsonSerializable(typeof(ApiResponse<CustomerDTO?>))]
    [JsonSerializable(typeof(ApiResponse<IEnumerable<CustomerDTO>>))]
    [JsonSerializable(typeof(ApiResponse<ReservationDTO?>))]
    [JsonSerializable(typeof(ApiResponse<IEnumerable<ReservationDTO>>))]
    [JsonSerializable(typeof(ApiResponse<AgendaDTO>))]
    [JsonSerializable(typeof(ApiResponse<AvailabilityDTO>))]
    [JsonSerializable(typeof(ApiResponse<ReservationSummaryDTO>))]
    [JsonSerializable(typeof(ApiResponse<string>))]
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(object))]
    public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
    {
    }
}
