using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Handlers;

public class AuthHandler
{
    private readonly IAuthService _authService;
    public AuthHandler(IAuthService authService) => _authService = authService;

    /// <summary>POST /auth/register — Registra un nuevo negocio (tenant) con su usuario owner.</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/auth/register")]
    public async Task<IHttpResult> Register([FromBody] RegisterTenantRequest body, ILambdaContext context)
    {
        try
        {
            var tenantId = await _authService.RegisterTenantAsync(body);
            return HttpResults.Created($"/tenants/{tenantId}", new { tenantId });
        }
        catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
        catch (Exception ex)
        {
            context.Logger.LogError($"[AuthHandler.Register] {ex.Message}");
            return HttpResults.InternalServerError("Error al registrar el negocio");
        }
    }

    /// <summary>POST /auth/login — Autentica un usuario y retorna JWT.</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/auth/login")]
    public async Task<IHttpResult> Login([FromBody] LoginRequest body, ILambdaContext context)
    {
        try
        {
            var token = await _authService.LoginAsync(body);
            return HttpResults.Ok(token);
        }
        catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
        catch (Exception ex)
        {
            context.Logger.LogError($"[AuthHandler.Login] {ex.Message}");
            return HttpResults.InternalServerError("Error al autenticar");
        }
    }
}
