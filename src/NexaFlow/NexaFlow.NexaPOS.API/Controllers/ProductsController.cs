using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;
using NexaFlow.NexaPOS.Domain.Exceptions;
using Prometheus;

namespace NexaFlow.NexaPOS.API.Controllers;

[ApiController]
[Route("products")]
[Produces("application/json")]
public class ProductsController(IProductService productService) : ControllerBase
{
    private static readonly Counter ProductsCreated = Metrics.CreateCounter(
        "nexaflow_products_created_total",
        "Número total de productos creados en el catálogo",
        new CounterConfiguration { LabelNames = ["tenant_id"] });

    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] CreateProductRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var id = await productService.CreateAsync(tenantId, body);
            ProductsCreated.WithLabels(tenantId.ToString()).Inc();
            return Created($"/products/{id}", id);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception) { return StatusCode(500, ApiResponse<object>.Fail("PRODUCT_CREATE_ERROR", "Error al crear producto")); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id,
        [FromBody] UpdateProductRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var productId))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "ID de producto inválido."));
        try
        {
            await productService.UpdateAsync(tenantId, productId, body);
            return Ok(ApiResponse<object>.Ok(new { message = "Producto actualizado correctamente.", id = productId }));
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception) { return StatusCode(500, ApiResponse<object>.Fail("PRODUCT_UPDATE_ERROR", "Error al actualizar producto")); }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try { return Ok(await productService.GetPagedAsync(tenantId, page, pageSize)); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception) { return StatusCode(500, ApiResponse<object>.Fail("PRODUCT_LIST_ERROR", "Error al listar productos")); }
    }
}
