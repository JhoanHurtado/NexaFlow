using Amazon.Lambda.APIGatewayEvents;
using NexaFlow.NexaInsight.Application.Dto;
using System.Text.Json.Serialization;

namespace NexaFlow.NexaInsight;

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(AverageTicketDto))]
[JsonSerializable(typeof(CancellationRateDto))]
[JsonSerializable(typeof(DailySummaryDto))]
[JsonSerializable(typeof(TopProductDto))]
[JsonSerializable(typeof(LowStockProductDto))]
[JsonSerializable(typeof(IEnumerable<DailySummaryDto>))]
[JsonSerializable(typeof(IEnumerable<TopProductDto>))]
[JsonSerializable(typeof(IEnumerable<LowStockProductDto>))]
[JsonSerializable(typeof(ApiResponse<AverageTicketDto>))]
[JsonSerializable(typeof(ApiResponse<CancellationRateDto>))]
[JsonSerializable(typeof(ApiResponse<IEnumerable<DailySummaryDto>>))]
[JsonSerializable(typeof(ApiResponse<IEnumerable<TopProductDto>>))]
[JsonSerializable(typeof(ApiResponse<IEnumerable<LowStockProductDto>>))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ApiResponse<object>))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}
