using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Handlers
{
    public class SaleHandler
    {
        private readonly ISaleService _saleService;
        public SaleHandler(ISaleService saleService) => _saleService = saleService;

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/sales")]
        public async Task<IHttpResult> CreateSale(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateSaleRequest body,
            ILambdaContext context)
        {
            var sw = Log.StartTimer();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _saleService.CreateAsync(tenantId, body);
                Log.Info(context, "sale-create", "Sale created",
                    tenantId: tenantHeader, method: "POST", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds, extra: new { saleId = id });
                return HttpResults.Created($"/sales/{id}", id);
            }
            catch (DomainException ex)
            {
                Log.Warn(context, "sale-create", ex.Message,
                    tenantId: tenantHeader, method: "POST", path: "/sales");
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(context, "sale-create", "Unhandled error creating sale",
                    ex: ex, tenantId: tenantHeader, method: "POST", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds);
                return HttpResults.InternalServerError("Error al crear la venta");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/sales/{id}")]
        public async Task<IHttpResult> GetSaleById(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            ILambdaContext context)
        {
            var sw = Log.StartTimer();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _saleService.GetSaleByIdAsync(tenantId, Guid.Parse(id));
                if (result.Data is null)
                {
                    Log.Warn(context, "sale-get", "Sale not found",
                        tenantId: tenantHeader, method: "GET", path: $"/sales/{id}");
                    return HttpResults.NotFound();
                }
                Log.Info(context, "sale-get", "Sale retrieved",
                    tenantId: tenantHeader, method: "GET", path: $"/sales/{id}",
                    durationMs: sw.ElapsedMilliseconds, extra: new { saleId = id });
                return HttpResults.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(context, "sale-get", "Unhandled error retrieving sale",
                    ex: ex, tenantId: tenantHeader, method: "GET", path: $"/sales/{id}",
                    durationMs: sw.ElapsedMilliseconds);
                return HttpResults.InternalServerError("Error al obtener la venta");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/sales")]
        public async Task<IHttpResult> ListSales(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var sw = Log.StartTimer();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _saleService.ListSalesAsync(tenantId, page, pageSize);
                Log.Info(context, "sale-list", "Sales listed",
                    tenantId: tenantHeader, method: "GET", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds, extra: new { page, pageSize });
                return HttpResults.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(context, "sale-list", "Unhandled error listing sales",
                    ex: ex, tenantId: tenantHeader, method: "GET", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds);
                return HttpResults.InternalServerError("Error al listar ventas");
            }
        }
    }
}
