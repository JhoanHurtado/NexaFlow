using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Handlers
{
    public class CustomerHandler
    {
        private readonly ICustomerService _customerService;

        public CustomerHandler(ICustomerService customerService) => _customerService = customerService;

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/customers")]
        public async Task<IHttpResult> RegisterCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateCustomerRequest body,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            try
            {
                var id = await _customerService.RegisterAsync(tenantId, body);
                return Api.Created($"/customers/{id}", new IdResponse(id));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.Register] {ex.Message}");
                return Api.InternalServerError("CUSTOMER_REGISTER_ERROR", "Error al registrar cliente");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Put, "/customers/{id}")]
        public async Task<IHttpResult> UpdateCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] UpdateCustomerRequest body,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var customerId, out var ie)) return ie!;
            try
            {
                await _customerService.UpdateAsync(tenantId, customerId, body);
                return Api.Ok(new IdResponse(customerId));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.Update] {ex.Message}");
                return Api.InternalServerError("CUSTOMER_UPDATE_ERROR", "Error al actualizar cliente");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/customers/{id}")]
        public async Task<IHttpResult> GetCustomerById(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var customerId, out var ie)) return ie!;
            try
            {
                var result = await _customerService.GetByIdAsync(tenantId, customerId);
                return result.Data is null
                    ? Api.NotFound("CUSTOMER_NOT_FOUND", "Cliente no encontrado")
                    : Api.Ok(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.GetById] {ex.Message}");
                return Api.InternalServerError("CUSTOMER_GET_ERROR", "Error al obtener cliente");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/customers")]
        public async Task<IHttpResult> ListCustomers(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (page < 1) return Api.BadRequest("VALIDATION_ERROR", "El parámetro 'page' debe ser mayor o igual a 1.");
            if (pageSize < 1 || pageSize > 100) return Api.BadRequest("VALIDATION_ERROR", "El parámetro 'pageSize' debe estar entre 1 y 100.");
            try
            {
                var result = await _customerService.ListAsync(tenantId, page, pageSize);
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.List] {ex.Message}");
                return Api.InternalServerError("CUSTOMER_LIST_ERROR", "Error al listar clientes");
            }
        }
    }
}
