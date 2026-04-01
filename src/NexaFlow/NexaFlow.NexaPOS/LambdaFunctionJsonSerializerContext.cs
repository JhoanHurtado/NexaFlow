using Amazon.Lambda.APIGatewayEvents;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Records.Create;
using System.Text.Json.Serialization;

namespace NexaFlow.NexaPOS
{
    [JsonSerializable(typeof(APIGatewayProxyRequest))]
    [JsonSerializable(typeof(APIGatewayProxyResponse))]
    [JsonSerializable(typeof(CreateProductRequest))]
    [JsonSerializable(typeof(CreateSaleRequest))]
    [JsonSerializable(typeof(CreateSaleItemRequest))]
    [JsonSerializable(typeof(CreateCustomerRequest))]
    [JsonSerializable(typeof(ProductDTO))]
    [JsonSerializable(typeof(SaleDTO))]
    [JsonSerializable(typeof(SaleItemDTO))]
    [JsonSerializable(typeof(CustomerDTO))]
    [JsonSerializable(typeof(PaginationMetadata))]
    [JsonSerializable(typeof(TenantConfigDTO))]
    [JsonSerializable(typeof(UpdateTenantConfigRequest))]
    [JsonSerializable(typeof(ApiResponse<TenantConfigDTO>))]
    [JsonSerializable(typeof(ApiResponse<IEnumerable<ProductDTO>>))]
    [JsonSerializable(typeof(ApiResponse<IEnumerable<SaleDTO>>))]
    [JsonSerializable(typeof(ApiResponse<IEnumerable<CustomerDTO>>))]
    [JsonSerializable(typeof(ApiResponse<SaleDTO?>))]
    [JsonSerializable(typeof(ApiResponse<Guid>))]
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(ErrorResponse))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(object))]
    public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
    {
    }
}
