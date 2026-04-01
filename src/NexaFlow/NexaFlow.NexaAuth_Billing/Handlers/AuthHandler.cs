using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Handlers;

public class AuthHandler
{
    private readonly IAuthService _authService;
    private readonly ITenantRepository _tenantRepository;

    public AuthHandler(IAuthService authService, ITenantRepository tenantRepository)
    {
        _authService = authService;
        _tenantRepository = tenantRepository;
    }

    /// <summary>Endpoint público — retorna nombre del tenant para el portal de reservas.</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/tenants/{id}")]
    public async Task<IHttpResult> GetTenantInfo(string id, ILambdaContext context)
    {
        if (!Validate.TryParseGuid(id, "id", out var tenantId, out var ve)) return ve!;
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant is null) return Api.NotFound("TENANT_NOT_FOUND", "Negocio no encontrado");
            return Api.Ok(ApiResponse<TenantInfoResponse>.Ok(new TenantInfoResponse(tenant.Id, tenant.Name)));
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"[AuthHandler.GetTenantInfo] {ex.Message}");
            return Api.InternalServerError("TENANT_GET_ERROR", "Error al obtener información del negocio");
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/auth/register")]
    public async Task<IHttpResult> Register([FromBody] RegisterTenantRequest body, ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = await _authService.RegisterTenantAsync(body);
            var idStr = tenantId.ToString();
            Log.Info(context, "tenant-register", "Tenant registered",
                method: "POST", path: "/auth/register",
                durationMs: sw.ElapsedMilliseconds,
                extra: w => w.WriteString("tenantId", idStr));
            return Api.Created($"/tenants/{tenantId}", new TenantCreatedResponse(tenantId));
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "tenant-register", ex.Message, method: "POST", path: "/auth/register");
            return Api.BadRequest("DOMAIN_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "tenant-register", "Unhandled error registering tenant",
                ex: ex, method: "POST", path: "/auth/register", durationMs: sw.ElapsedMilliseconds);
            return Api.InternalServerError("REGISTER_ERROR", "Error al registrar el negocio");
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/auth/login")]
    public async Task<IHttpResult> Login([FromBody] LoginRequest body, ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var token = await _authService.LoginAsync(body);
            Log.Info(context, "auth-login", "Login successful",
                method: "POST", path: "/auth/login", durationMs: sw.ElapsedMilliseconds);
            return Api.Ok(token);
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "auth-login", ex.Message, method: "POST", path: "/auth/login");
            return Api.BadRequest("DOMAIN_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "auth-login", "Unhandled error during login",
                ex: ex, method: "POST", path: "/auth/login", durationMs: sw.ElapsedMilliseconds);
            return Api.InternalServerError("LOGIN_ERROR", "Error al autenticar");
        }
    }
}
