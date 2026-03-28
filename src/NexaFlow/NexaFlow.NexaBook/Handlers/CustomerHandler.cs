using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Handlers
{
    /// <summary>
    /// Handler Lambda para operaciones sobre clientes de NexaBook.
    /// Requiere el header <c>x-tenant-id</c> en todos los requests.
    /// </summary>
    public class CustomerHandler
    {
        private readonly ICustomerService _customerService;

        public CustomerHandler(ICustomerService customerService) => _customerService = customerService;

        /// <summary>Registra un nuevo cliente (auto-registro o por staff).</summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/customers")]
        public async Task<IHttpResult> RegisterCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateCustomerRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _customerService.RegisterAsync(tenantId, body);
                return Api.Created($"/customers/{id}", new { id });
            }
            catch (DomainException ex) { return Api.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.Register] {ex.Message}");
                return Api.InternalServerError("Error al registrar cliente");
            }
        }

        /// <summary>Actualiza los datos de contacto de un cliente.</summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Put, "/customers/{id}")]
        public async Task<IHttpResult> UpdateCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] UpdateCustomerRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                await _customerService.UpdateAsync(tenantId, Guid.Parse(id), body);
                return Api.Ok(new { id });
            }
            catch (DomainException ex) { return Api.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.Update] {ex.Message}");
                return Api.InternalServerError("Error al actualizar cliente");
            }
        }

        /// <summary>Obtiene un cliente por ID.</summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/customers/{id}")]
        public async Task<IHttpResult> GetCustomerById(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _customerService.GetByIdAsync(tenantId, Guid.Parse(id));
                return result.Data is null ? Api.NotFound() : Api.Ok(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.GetById] {ex.Message}");
                return Api.InternalServerError("Error al obtener cliente");
            }
        }

        /// <summary>Lista clientes del tenant con paginación.</summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/customers")]
        public async Task<IHttpResult> ListCustomers(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _customerService.ListAsync(tenantId, page, pageSize);
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.List] {ex.Message}");
                return Api.InternalServerError("Error al listar clientes");
            }
        }
    }
}
