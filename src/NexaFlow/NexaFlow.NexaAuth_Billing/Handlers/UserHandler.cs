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

    /// <summary>POST /users — Crea un usuario en el tenant del header.</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/users")]
    public async Task<IHttpResult> CreateUser(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromBody] CreateUserRequest body,
        ILambdaContext context)
    {
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var id = await _userService.CreateAsync(tenantId, body);
            return HttpResults.Created($"/users/{id}", new { id });
        }
        catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
        catch (Exception ex)
        {
            context.Logger.LogError($"[UserHandler.CreateUser] {ex.Message}");
            return HttpResults.InternalServerError("Error al crear usuario");
        }
    }

    /// <summary>GET /users — Lista usuarios del tenant.</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/users")]
    public async Task<IHttpResult> ListUsers(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        ILambdaContext context)
    {
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var users = await _userService.ListAsync(tenantId);
            return HttpResults.Ok(users);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"[UserHandler.ListUsers] {ex.Message}");
            return HttpResults.InternalServerError("Error al listar usuarios");
        }
    }

    /// <summary>DELETE /users/{id} — Desactiva un usuario del tenant.</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Delete, "/users/{id}")]
    public async Task<IHttpResult> DeactivateUser(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        string id,
        ILambdaContext context)
    {
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            await _userService.DeactivateAsync(tenantId, Guid.Parse(id));
            return HttpResults.Ok(new { message = "Usuario desactivado" });
        }
        catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
        catch (Exception ex)
        {
            context.Logger.LogError($"[UserHandler.DeactivateUser] {ex.Message}");
            return HttpResults.InternalServerError("Error al desactivar usuario");
        }
    }
}
