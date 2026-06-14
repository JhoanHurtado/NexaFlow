using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;
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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var err)) return err!;
            try
            {
                var id = await _productService.CreateAsync(tenantId, body);
                Log.Info(context, "product-create", "Product created", tenantId: tenantHeader, method: "POST", path: "/products", durationMs: sw.ElapsedMilliseconds);
                return Api.Created($"/products/{id}", id);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex) { Log.Error(context, "product-create", "Error", ex: ex, tenantId: tenantHeader, method: "POST", path: "/products", durationMs: sw.ElapsedMilliseconds); return Api.InternalServerError("PRODUCT_CREATE_ERROR", "Error al crear producto"); }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Put, "/products/{id}")]
        public async Task<IHttpResult> Update(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] UpdateProductRequest body,
            ILambdaContext context)
        {
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var err)) return err!;
            if (!Guid.TryParse(id, out var productId)) return Api.BadRequest("VALIDATION_ERROR", "ID de producto inválido.");
            try
            {
                await _productService.UpdateAsync(tenantId, productId, body);
                Log.Info(context, "product-update", "Product updated", tenantId: tenantHeader, method: "PUT", path: $"/products/{id}", durationMs: sw.ElapsedMilliseconds);
                return Api.Ok(ApiResponse<UpdatedResponse>.Ok(new UpdatedResponse("Producto actualizado correctamente.", productId)));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex) { Log.Error(context, "product-update", "Error", ex: ex, tenantId: tenantHeader, method: "PUT", path: $"/products/{id}", durationMs: sw.ElapsedMilliseconds); return Api.InternalServerError("PRODUCT_UPDATE_ERROR", "Error al actualizar producto"); }
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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var err)) return err!;
            if (page < 1 || pageSize < 1 || pageSize > 100) return Api.BadRequest("VALIDATION_ERROR", "Parámetros de paginación inválidos.");
            try
            {
                var response = await _productService.GetPagedAsync(tenantId, page, pageSize);
                Log.Info(context, "product-list", "Products listed", tenantId: tenantHeader, method: "GET", path: "/products", durationMs: sw.ElapsedMilliseconds);
                return Api.Ok(response);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex) { Log.Error(context, "product-list", "Error", ex: ex, tenantId: tenantHeader, method: "GET", path: "/products", durationMs: sw.ElapsedMilliseconds); return Api.InternalServerError("PRODUCT_LIST_ERROR", "Error al listar productos"); }
        }
    }
}
