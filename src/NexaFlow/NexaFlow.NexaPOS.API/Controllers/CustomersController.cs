using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.API.Controllers;

[ApiController]
[Route("customers")]
[Produces("application/json")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCustomer(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] CreateCustomerRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var id = await customerService.CreateAsync(tenantId, body);
            return Created($"/customers/{id}", id);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_CREATE_ERROR", "Error al crear cliente"));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListCustomers(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (page < 1)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'page' debe ser mayor o igual a 1."));
        if (pageSize < 1 || pageSize > 100)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'pageSize' debe estar entre 1 y 100."));
        try
        {
            var result = await customerService.ListCustomersAsync(tenantId, page, pageSize);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_LIST_ERROR", "Error al listar clientes"));
        }
    }
}
