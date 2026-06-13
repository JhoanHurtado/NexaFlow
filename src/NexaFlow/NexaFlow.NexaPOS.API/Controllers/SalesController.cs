using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;
using NexaFlow.NexaPOS.Domain.Exceptions;
using Prometheus;

namespace NexaFlow.NexaPOS.API.Controllers;

[ApiController]
[Route("sales")]
[Produces("application/json")]
public class SalesController(ISaleService saleService) : ControllerBase
{
    private static readonly Counter SalesCreated = Metrics.CreateCounter(
        "nexaflow_sales_created_total",
        "Número total de ventas creadas",
        new CounterConfiguration { LabelNames = ["tenant_id"] });

    private static readonly Counter SalesCancelled = Metrics.CreateCounter(
        "nexaflow_sales_cancelled_total",
        "Número total de ventas canceladas o anuladas",
        new CounterConfiguration { LabelNames = ["tenant_id"] });

    private static readonly Counter SalesCompleted = Metrics.CreateCounter(
        "nexaflow_sales_completed_total",
        "Número total de ventas completadas/pagadas",
        new CounterConfiguration { LabelNames = ["tenant_id"] });

    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSale(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] CreateSaleRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var id = await saleService.CreateAsync(tenantId, body);
            SalesCreated.WithLabels(tenantId.ToString()).Inc();
            return Created($"/sales/{id}", id);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("SALE_CREATE_ERROR", "Error al crear la venta"));
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SaleDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSaleById(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var saleId))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"El parámetro 'id' no tiene un formato válido."));
        try
        {
            var result = await saleService.GetSaleByIdAsync(tenantId, saleId);
            if (result.Data is null)
                return NotFound(ApiResponse<object>.Fail("SALE_NOT_FOUND", "Venta no encontrada"));
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("SALE_GET_ERROR", "Error al obtener la venta"));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SaleDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListSales(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (page < 1)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'page' debe ser mayor o igual a 1."));
        if (pageSize < 1 || pageSize > 200)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'pageSize' debe estar entre 1 y 200."));
        try
        {
            var result = await saleService.ListSalesAsync(tenantId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("SALE_LIST_ERROR", "Error al listar ventas"));
        }
    }

    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStatus(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id,
        [FromBody] UpdateSaleStatusRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var saleId))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'id' no tiene un formato válido."));
        try
        {
            await saleService.UpdateStatusAsync(tenantId, saleId, body.Status);
            if (body.Status?.ToLower() is "cancelled" or "voided")
                SalesCancelled.WithLabels(tenantId.ToString()).Inc();
            else if (body.Status?.ToLower() is "completed" or "paid")
                SalesCompleted.WithLabels(tenantId.ToString()).Inc();
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.Fail("SALE_STATUS_ERROR", "Error al actualizar el estado"));
        }
    }
}
