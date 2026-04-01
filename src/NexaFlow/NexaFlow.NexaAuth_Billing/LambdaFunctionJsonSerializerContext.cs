using Amazon.Lambda.APIGatewayEvents;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Records;
using System.Text.Json.Serialization;

namespace NexaFlow.NexaAuth_Billing;

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(RegisterTenantRequest))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(AuthTokenDto))]
[JsonSerializable(typeof(TenantDto))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(IEnumerable<UserDto>))]
[JsonSerializable(typeof(SubscriptionDto))]
[JsonSerializable(typeof(ApiResponse<AuthTokenDto>))]
[JsonSerializable(typeof(ApiResponse<SubscriptionDto>))]
[JsonSerializable(typeof(ApiResponse<IEnumerable<UserDto>>))]
[JsonSerializable(typeof(TenantCreatedResponse))]
[JsonSerializable(typeof(UserCreatedResponse))]
[JsonSerializable(typeof(MessageResponse))]
[JsonSerializable(typeof(WebhookReceivedResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(IEnumerable<PlanRecord>))]
[JsonSerializable(typeof(ApiResponse<IEnumerable<PlanRecord>>))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}
