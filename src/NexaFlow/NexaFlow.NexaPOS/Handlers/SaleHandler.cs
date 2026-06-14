using NexaFlow.NexaPOS.Application.Dto;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;
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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            try
            {
                var id = await _saleService.CreateAsync(tenantId, body);
                var idStr = id.ToString();
                Log.Info(context, "sale-create", "Sale created",
                    tenantId: tenantHeader, method: "POST", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => w.WriteString("saleId", idStr));
                return Api.Created($"/sales/{id}", id);
            }
            catch (DomainException ex)
            {
                Log.Warn(context, "sale-create", ex.Message,
                    tenantId: tenantHeader, method: "POST", path: "/sales");
                return Api.BadRequest("DOMAIN_ERROR", ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(context, "sale-create", "Unhandled error creating sale",
                    ex: ex, tenantId: tenantHeader, method: "POST", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds);
                return Api.InternalServerError("SALE_CREATE_ERROR", "Error al crear la venta");
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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            if (!Validate.TryParseGuid(id, "id", out var saleId, out var idError))
                return idError!;
            try
            {
                var result = await _saleService.GetSaleByIdAsync(tenantId, saleId);
                if (result.Data is null)
                {
                    Log.Warn(context, "sale-get", "Sale not found",
                        tenantId: tenantHeader, method: "GET", path: $"/sales/{id}");
                    return Api.NotFound("SALE_NOT_FOUND", "Venta no encontrada");
                }
                Log.Info(context, "sale-get", "Sale retrieved",
                    tenantId: tenantHeader, method: "GET", path: $"/sales/{id}",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => w.WriteString("saleId", id));
                return Api.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(context, "sale-get", "Unhandled error retrieving sale",
                    ex: ex, tenantId: tenantHeader, method: "GET", path: $"/sales/{id}",
                    durationMs: sw.ElapsedMilliseconds);
                return Api.InternalServerError("SALE_GET_ERROR", "Error al obtener la venta");
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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            if (page < 1)    return Api.BadRequest("VALIDATION_ERROR", "El parámetro 'page' debe ser mayor o igual a 1.");
            if (pageSize < 1 || pageSize > 200) return Api.BadRequest("VALIDATION_ERROR", "El parámetro 'pageSize' debe estar entre 1 y 200.");
            try
            {
                var result = await _saleService.ListSalesAsync(tenantId, page, pageSize);
                Log.Info(context, "sale-list", "Sales listed",
                    tenantId: tenantHeader, method: "GET", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => { w.WriteNumber("page", page); w.WriteNumber("pageSize", pageSize); });
                return Api.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(context, "sale-list", "Unhandled error listing sales",
                    ex: ex, tenantId: tenantHeader, method: "GET", path: "/sales",
                    durationMs: sw.ElapsedMilliseconds);
                return Api.InternalServerError("SALE_LIST_ERROR", "Error al listar ventas");
            }
        }
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Patch, "/sales/{id}/status")]
        public async Task<IHttpResult> UpdateSaleStatus(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] UpdateSaleStatusRequest body,
            ILambdaContext context)
        {
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            if (!Validate.TryParseGuid(id, "id", out var saleId, out var idError))
                return idError!;
            try
            {
                await _saleService.UpdateStatusAsync(tenantId, saleId, body.Status);
                Log.Info(context, "sale-status", "Sale status updated",
                    tenantId: tenantHeader, method: "PATCH", path: $"/sales/{id}/status",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => { w.WriteString("saleId", id); w.WriteString("status", body.Status); });
                return Api.Ok(ApiResponse<object>.Ok(new { updated = true, saleId = id, status = body.Status }));
            }
            catch (DomainException ex)
            {
                Log.Warn(context, "sale-status", ex.Message, tenantId: tenantHeader, method: "PATCH", path: $"/sales/{id}/status");
                return Api.BadRequest("DOMAIN_ERROR", ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(context, "sale-status", "Unhandled error updating sale status",
                    ex: ex, tenantId: tenantHeader, method: "PATCH", path: $"/sales/{id}/status",
                    durationMs: sw.ElapsedMilliseconds);
                return Api.InternalServerError("SALE_STATUS_ERROR", "Error al actualizar el estado");
            }
        }
    }
}
