using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaAuth_Billing.Application.Dto;
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
        if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
        try
        {
            var id = await _userService.CreateAsync(tenantId, body);
            var idStr = id.ToString();
            Log.Info(context, "user-create", "User created",
                tenantId: tenantHeader, method: "POST", path: "/users",
                durationMs: sw.ElapsedMilliseconds,
                extra: w => w.WriteString("userId", idStr));
            return HttpResults.Created($"/users/{id}", new UserCreatedResponse(id));
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
            return HttpResults.InternalServerError(new ErrorResponse("USER_CREATE_ERROR", "Error al crear usuario"));
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/users")]
    public async Task<IHttpResult> ListUsers(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
        try
        {
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
            return HttpResults.InternalServerError(new ErrorResponse("USER_LIST_ERROR", "Error al listar usuarios"));
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
        if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
        if (!Validate.TryParseGuid(id, "id", out var userId, out var ue)) return ue!;
        try
        {
            await _userService.DeactivateAsync(tenantId, userId);
            Log.Info(context, "user-deactivate", "User deactivated",
                tenantId: tenantHeader, method: "DELETE", path: $"/users/{id}",
                durationMs: sw.ElapsedMilliseconds,
                extra: w => w.WriteString("userId", id));
            return HttpResults.Ok(new MessageResponse("Usuario desactivado"));
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
            return HttpResults.InternalServerError(new ErrorResponse("USER_DEACTIVATE_ERROR", "Error al desactivar usuario"));
        }
    }
}
