using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaInsight.Application.Dto;
using NexaFlow.NexaInsight.Application.Interfaces.Services;
using NexaFlow.NexaInsight.Domain.Exceptions;

namespace NexaFlow.NexaInsight.API.Controllers;

[ApiController]
[Route("insights")]
[Produces("application/json")]
public class InsightsController(IInsightService insightService) : ControllerBase
{
    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    private IActionResult DateError(string param) =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"El parámetro '{param}' no tiene un formato de fecha válido (yyyy-MM-dd)."));

    [HttpGet("average-ticket")]
    [ProducesResponseType(typeof(ApiResponse<AverageTicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAverageTicket(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!DateOnly.TryParse(from, out var fromDate)) return DateError("from");
        if (!DateOnly.TryParse(to, out var toDate)) return DateError("to");
        if (fromDate > toDate)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'from' no puede ser posterior a 'to'."));
        try
        {
            var result = await insightService.GetAverageTicketAsync(tenantId, fromDate, toDate);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("AVG_TICKET_ERROR", "Error al calcular ticket promedio"));
        }
    }

    [HttpGet("cancellation-rate")]
    [ProducesResponseType(typeof(ApiResponse<CancellationRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCancellationRate(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!DateOnly.TryParse(from, out var fromDate)) return DateError("from");
        if (!DateOnly.TryParse(to, out var toDate)) return DateError("to");
        if (fromDate > toDate)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'from' no puede ser posterior a 'to'."));
        try
        {
            var result = await insightService.GetCancellationRateAsync(tenantId, fromDate, toDate);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CANCELLATION_RATE_ERROR", "Error al calcular tasa de cancelación"));
        }
    }

    [HttpGet("daily-summary")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DailySummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDailySummary(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!DateOnly.TryParse(from, out var fromDate)) return DateError("from");
        if (!DateOnly.TryParse(to, out var toDate)) return DateError("to");
        if (fromDate > toDate)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'from' no puede ser posterior a 'to'."));
        try
        {
            var result = await insightService.GetDailySummaryAsync(tenantId, fromDate, toDate);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("DAILY_SUMMARY_ERROR", "Error al obtener resumen diario"));
        }
    }
}
