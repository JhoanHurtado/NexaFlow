using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;
using NexaFlow.NexaPOS.Domain.Exceptions;
using Prometheus;

namespace NexaFlow.NexaPOS.API.Controllers;

[ApiController]
[Route("customers")]
[Produces("application/json")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    private static readonly Counter PosCustomersRegistered = Metrics.CreateCounter(
        "nexaflow_pos_customers_registered_total",
        "Número total de clientes registrados en el POS",
        new CounterConfiguration { LabelNames = ["tenant_id"] });

    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpPost]
    public async Task<IActionResult> CreateCustomer(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] CreateCustomerRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var id = await customerService.CreateAsync(tenantId, body);
            PosCustomersRegistered.WithLabels(tenantId.ToString()).Inc();
            return Created($"/customers/{id}", id);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception) { return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_CREATE_ERROR", "Error al crear cliente")); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id,
        [FromBody] UpdateCustomerRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var customerId))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "ID de cliente inválido."));
        try
        {
            await customerService.UpdateAsync(tenantId, customerId, body);
            return Ok(ApiResponse<object>.Ok(new { message = "Cliente actualizado correctamente.", id = customerId }));
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception) { return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_UPDATE_ERROR", "Error al actualizar cliente")); }
    }

    [HttpGet]
    public async Task<IActionResult> ListCustomers(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try { return Ok(await customerService.ListCustomersAsync(tenantId, page, pageSize)); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception) { return StatusCode(500, ApiResponse<object>.Fail("CUSTOMER_LIST_ERROR", "Error al listar clientes")); }
    }
}
