using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.API.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(TenantCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest body)
    {
        try
        {
            var tenantId = await authService.RegisterTenantAsync(body);
            return CreatedAtAction(nameof(Register), new TenantCreatedResponse(tenantId));
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("REGISTER_ERROR", "Error al registrar el negocio"));
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest body)
    {
        try
        {
            var token = await authService.LoginAsync(body);
            return Ok(token);
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("LOGIN_ERROR", "Error al autenticar"));
        }
    }
}
