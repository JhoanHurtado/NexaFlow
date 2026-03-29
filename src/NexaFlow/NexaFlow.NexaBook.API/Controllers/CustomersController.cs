using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.API.Controllers;

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
    public async Task<IActionResult> RegisterCustomer(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] CreateCustomerRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var id = await customerService.RegisterAsync(tenantId, body);
            return Created($"/customers/{id}", new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_REGISTER_ERROR", "Error al registrar cliente"));
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCustomer(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id,
        [FromBody] UpdateCustomerRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var customerId))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'id' no tiene un formato válido."));
        try
        {
            await customerService.UpdateAsync(tenantId, customerId, body);
            return Ok(new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_UPDATE_ERROR", "Error al actualizar cliente"));
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCustomerById(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var customerId))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'id' no tiene un formato válido."));
        try
        {
            var result = await customerService.GetByIdAsync(tenantId, customerId);
            return result.Data is null
                ? NotFound(ApiResponse<object>.Fail("CUSTOMER_NOT_FOUND", "Cliente no encontrado"))
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_GET_ERROR", "Error al obtener cliente"));
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
            var result = await customerService.ListAsync(tenantId, page, pageSize);
            return Ok(result);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_LIST_ERROR", "Error al listar clientes"));
        }
    }
}
