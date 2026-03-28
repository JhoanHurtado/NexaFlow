using NexaFlow.NexaPOS.Application.Dto;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Handlers
{
    public class ProductHandler
    {
        private readonly IProductService _productService;
        public ProductHandler(IProductService productService) => _productService = productService;

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/products")]
        public async Task<IHttpResult> Create(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateProductRequest body,
            ILambdaContext context)
        {
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            try
            {
                var id = await _productService.CreateAsync(tenantId, body);
                var idStr = id.ToString();
                Log.Info(context, "product-create", "Product created",
                    tenantId: tenantHeader, method: "POST", path: "/products",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => w.WriteString("productId", idStr));
                return HttpResults.Created($"/products/{id}", id);
            }
            catch (DomainException ex)
            {
                Log.Warn(context, "product-create", ex.Message,
                    tenantId: tenantHeader, method: "POST", path: "/products");
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(context, "product-create", "Unhandled error creating product",
                    ex: ex, tenantId: tenantHeader, method: "POST", path: "/products",
                    durationMs: sw.ElapsedMilliseconds);
                return HttpResults.InternalServerError(new ErrorResponse("PRODUCT_CREATE_ERROR", "Error al crear producto"));
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/products")]
        public async Task<IHttpResult> List(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            if (page < 1)    return HttpResults.BadRequest("El parámetro 'page' debe ser mayor o igual a 1.");
            if (pageSize < 1 || pageSize > 100) return HttpResults.BadRequest("El parámetro 'pageSize' debe estar entre 1 y 100.");
            try
            {
                var response = await _productService.GetPagedAsync(tenantId, page, pageSize);
                Log.Info(context, "product-list", "Products listed",
                    tenantId: tenantHeader, method: "GET", path: "/products",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => { w.WriteNumber("page", page); w.WriteNumber("pageSize", pageSize); });
                return HttpResults.Ok(response);
            }
            catch (DomainException ex)
            {
                Log.Warn(context, "product-list", ex.Message,
                    tenantId: tenantHeader, method: "GET", path: "/products");
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(context, "product-list", "Unhandled error listing products",
                    ex: ex, tenantId: tenantHeader, method: "GET", path: "/products",
                    durationMs: sw.ElapsedMilliseconds);
                return HttpResults.InternalServerError(new ErrorResponse("PRODUCT_LIST_ERROR", "Error al listar productos"));
            }
        }
    }
}
