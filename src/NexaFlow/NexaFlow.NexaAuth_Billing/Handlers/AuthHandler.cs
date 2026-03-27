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
            return HttpResults.Created($"/tenants/{tenantId}", new { tenantId });
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "tenant-register", ex.Message, method: "POST", path: "/auth/register");
            return HttpResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "tenant-register", "Unhandled error registering tenant",
                ex: ex, method: "POST", path: "/auth/register", durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError(new { code = "REGISTER_ERROR", message = "Error al registrar el negocio" });
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
            return HttpResults.Ok(token);
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "auth-login", ex.Message, method: "POST", path: "/auth/login");
            return HttpResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "auth-login", "Unhandled error during login",
                ex: ex, method: "POST", path: "/auth/login", durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError(new { code = "LOGIN_ERROR", message = "Error al autenticar" });
        }
    }
}
