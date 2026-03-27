using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Handlers;

public class UserHandler
{
    private readonly IUserService _userService;
    public UserHandler(IUserService userService) => _userService = userService;

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/users")]
    public async Task<IHttpResult> CreateUser(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromBody] CreateUserRequest body,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var id = await _userService.CreateAsync(tenantId, body);
            Log.Info(context, "user-create", "User created",
                tenantId: tenantHeader, method: "POST", path: "/users",
                durationMs: sw.ElapsedMilliseconds, extra: new { userId = id });
            return HttpResults.Created($"/users/{id}", new { id });
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "user-create", ex.Message,
                tenantId: tenantHeader, method: "POST", path: "/users");
            return HttpResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "user-create", "Unhandled error creating user",
                ex: ex, tenantId: tenantHeader, method: "POST", path: "/users",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError("Error al crear usuario");
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/users")]
    public async Task<IHttpResult> ListUsers(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var users = await _userService.ListAsync(tenantId);
            Log.Info(context, "user-list", "Users listed",
                tenantId: tenantHeader, method: "GET", path: "/users",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.Ok(users);
        }
        catch (Exception ex)
        {
            Log.Error(context, "user-list", "Unhandled error listing users",
                ex: ex, tenantId: tenantHeader, method: "GET", path: "/users",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError("Error al listar usuarios");
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Delete, "/users/{id}")]
    public async Task<IHttpResult> DeactivateUser(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        string id,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            await _userService.DeactivateAsync(tenantId, Guid.Parse(id));
            Log.Info(context, "user-deactivate", "User deactivated",
                tenantId: tenantHeader, method: "DELETE", path: $"/users/{id}",
                durationMs: sw.ElapsedMilliseconds, extra: new { userId = id });
            return HttpResults.Ok(new { message = "Usuario desactivado" });
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "user-deactivate", ex.Message,
                tenantId: tenantHeader, method: "DELETE", path: $"/users/{id}");
            return HttpResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "user-deactivate", "Unhandled error deactivating user",
                ex: ex, tenantId: tenantHeader, method: "DELETE", path: $"/users/{id}",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError("Error al desactivar usuario");
        }
    }
}
