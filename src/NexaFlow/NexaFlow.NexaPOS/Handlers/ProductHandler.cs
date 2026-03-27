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
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _productService.CreateAsync(tenantId, body);
                Log.Info(context, "product-create", "Product created",
                    tenantId: tenantHeader, method: "POST", path: "/products",
                    durationMs: sw.ElapsedMilliseconds, extra: new { productId = id });
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
                return HttpResults.InternalServerError("Error al crear producto");
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
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var response = await _productService.GetPagedAsync(tenantId, page, pageSize);
                Log.Info(context, "product-list", "Products listed",
                    tenantId: tenantHeader, method: "GET", path: "/products",
                    durationMs: sw.ElapsedMilliseconds, extra: new { page, pageSize });
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
                return HttpResults.InternalServerError("Error al listar productos");
            }
        }
    }
}
