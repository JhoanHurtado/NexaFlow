using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Handlers
{
    /// <summary>
    /// Handler Lambda para operaciones sobre productos del catálogo.
    /// Expone los endpoints <c>POST /products</c> y <c>GET /products</c> vía API Gateway REST.
    /// Requiere el header <c>x-tenant-id</c> en todos los requests.
    /// </summary>
    public class ProductHandler
    {
        private readonly IProductService _productService;

        /// <param name="productService">Servicio de productos inyectado por DI.</param>
        public ProductHandler(IProductService productService) => _productService = productService;

        /// <summary>
        /// Crea un nuevo producto con stock inicial para el tenant.
        /// Retorna 201 Created con la URL del recurso y el ID generado.
        /// Retorna 400 si alguna regla de negocio es violada.
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/products")]
        public async Task<IHttpResult> Create(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateProductRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _productService.CreateAsync(tenantId, body);
                return HttpResults.Created($"/products/{id}", id);
            }
            catch (DomainException ex)
            {
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ProductHandler.Create] {ex.Message}");
                return HttpResults.InternalServerError("Error al crear producto");
            }
        }

        /// <summary>
        /// Lista los productos activos del tenant con paginación.
        /// Retorna 200 con <c>ApiResponse&lt;IEnumerable&lt;ProductDTO&gt;&gt;</c>.
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/products")]
        public async Task<IHttpResult> List(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var response = await _productService.GetPagedAsync(tenantId, page, pageSize);
                return HttpResults.Ok(response);
            }
            catch (DomainException ex)
            {
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ProductHandler.List] {ex.Message}");
                return HttpResults.InternalServerError("Error al listar productos");
            }
        }
    }
}
