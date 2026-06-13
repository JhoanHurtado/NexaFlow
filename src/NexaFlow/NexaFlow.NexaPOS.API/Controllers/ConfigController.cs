using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.API.Controllers;

[ApiController]
[Route("config")]
[Produces("application/json")]
public class ConfigController(ITenantConfigService configService) : ControllerBase
{
    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TenantConfigDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConfig(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var result = await configService.GetAsync(tenantId);
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CONFIG_GET_ERROR", "Error al obtener configuración"));
        }
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<TenantConfigDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateConfig(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] UpdateTenantConfigRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var result = await configService.UpdateAsync(tenantId, body);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CONFIG_UPDATE_ERROR", "Error al actualizar configuración"));
        }
    }
}
