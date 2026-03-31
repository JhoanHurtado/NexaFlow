using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.API.Controllers;

[ApiController]
[Route("users")]
[Produces("application/json")]
public class UsersController(IUserService userService) : ControllerBase
{
    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpPost]
    [ProducesResponseType(typeof(UserCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUser(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] CreateUserRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var id = await userService.CreateAsync(tenantId, body);
            return CreatedAtAction(nameof(CreateUser), new UserCreatedResponse(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("USER_CREATE_ERROR", "Error al crear usuario"));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListUsers([FromHeader(Name = "x-tenant-id")] string? tenantHeader)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var users = await userService.ListAsync(tenantId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("USER_LIST_ERROR", "Error al listar usuarios"));
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateUser(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var userId))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"El parámetro 'id' no tiene un formato válido."));
        try
        {
            await userService.DeactivateAsync(tenantId, userId);
            return Ok(new MessageResponse("Usuario desactivado"));
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("USER_DEACTIVATE_ERROR", "Error al desactivar usuario"));
        }
    }
}
