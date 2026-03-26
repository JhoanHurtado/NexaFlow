using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Handlers
{
    /// <summary>
    /// Handler Lambda para operaciones sobre ventas.
    /// Expone los endpoints <c>POST /sales</c>, <c>GET /sales</c> y <c>GET /sales/{id}</c> vía API Gateway REST.
    /// Requiere el header <c>x-tenant-id</c> en todos los requests.
    /// </summary>
    public class SaleHandler
    {
        private readonly ISaleService _saleService;

        /// <param name="saleService">Servicio de ventas inyectado por DI.</param>
        public SaleHandler(ISaleService saleService) => _saleService = saleService;

        /// <summary>
        /// Crea una venta para el tenant. Valida stock, deduce inventario y encola eventos en una transacción atómica.
        /// Retorna 201 Created con el ID de la venta.
        /// Retorna 400 si hay errores de negocio (stock insuficiente, producto inactivo, etc.).
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/sales")]
        public async Task<IHttpResult> CreateSale(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateSaleRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _saleService.CreateAsync(tenantId, body);
                return HttpResults.Created($"/sales/{id}", id);
            }
            catch (DomainException ex)
            {
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[SaleHandler.CreateSale] {ex.Message}");
                return HttpResults.InternalServerError("Error al crear la venta");
            }
        }

        /// <summary>
        /// Obtiene una venta por su ID incluyendo sus ítems y nombres de productos.
        /// Retorna 200 con la venta, o 404 si no existe.
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/sales/{id}")]
        public async Task<IHttpResult> GetSaleById(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _saleService.GetSaleByIdAsync(tenantId, Guid.Parse(id));
                return result.Data is null ? HttpResults.NotFound() : HttpResults.Ok(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[SaleHandler.GetSaleById] {ex.Message}");
                return HttpResults.InternalServerError("Error al obtener la venta");
            }
        }

        /// <summary>
        /// Lista las ventas del tenant con paginación, ordenadas por fecha descendente.
        /// Retorna 200 con <c>ApiResponse&lt;IEnumerable&lt;SaleDTO&gt;&gt;</c>.
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/sales")]
        public async Task<IHttpResult> ListSales(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _saleService.ListSalesAsync(tenantId, page, pageSize);
                return HttpResults.Ok(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[SaleHandler.ListSales] {ex.Message}");
                return HttpResults.InternalServerError("Error al listar ventas");
            }
        }
    }
}
