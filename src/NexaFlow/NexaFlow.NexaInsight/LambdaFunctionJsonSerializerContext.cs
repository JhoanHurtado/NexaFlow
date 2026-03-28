using Amazon.Lambda.APIGatewayEvents;
using NexaFlow.NexaInsight.Application.Dto;
using System.Text.Json.Serialization;

namespace NexaFlow.NexaInsight;

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(AverageTicketDto))]
[JsonSerializable(typeof(CancellationRateDto))]
[JsonSerializable(typeof(DailySummaryDto))]
[JsonSerializable(typeof(IEnumerable<DailySummaryDto>))]
[JsonSerializable(typeof(ApiResponse<AverageTicketDto>))]
[JsonSerializable(typeof(ApiResponse<CancellationRateDto>))]
[JsonSerializable(typeof(ApiResponse<IEnumerable<DailySummaryDto>>))]
[JsonSerializable(typeof(ApiResponse<string>))]
[JsonSerializable(typeof(ApiResponse<object>))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}
