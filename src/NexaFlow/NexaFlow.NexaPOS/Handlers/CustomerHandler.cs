using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Handlers
{
    public class CustomerHandler
    {
        private readonly ICustomerService _customerService;
        public CustomerHandler(ICustomerService customerService) => _customerService = customerService;

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/customers")]
        public async Task<IHttpResult> CreateCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateCustomerRequest body,
            ILambdaContext context)
        {
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var err)) return err!;
            try
            {
                var id = await _customerService.CreateAsync(tenantId, body);
                Log.Info(context, "customer-create", "Customer created", tenantId: tenantHeader, method: "POST", path: "/customers", durationMs: sw.ElapsedMilliseconds);
                return Api.Created($"/customers/{id}", id);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex) { Log.Error(context, "customer-create", "Error", ex: ex, tenantId: tenantHeader, method: "POST", path: "/customers", durationMs: sw.ElapsedMilliseconds); return Api.InternalServerError("CUSTOMER_CREATE_ERROR", "Error al crear cliente"); }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Put, "/customers/{id}")]
        public async Task<IHttpResult> UpdateCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] UpdateCustomerRequest body,
            ILambdaContext context)
        {
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var err)) return err!;
            if (!Guid.TryParse(id, out var customerId)) return Api.BadRequest("VALIDATION_ERROR", "ID de cliente inválido.");
            try
            {
                await _customerService.UpdateAsync(tenantId, customerId, body);
                Log.Info(context, "customer-update", "Customer updated", tenantId: tenantHeader, method: "PUT", path: $"/customers/{id}", durationMs: sw.ElapsedMilliseconds);
                return Api.Ok(ApiResponse<object>.Ok(new { message = "Cliente actualizado correctamente.", id = customerId }));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex) { Log.Error(context, "customer-update", "Error", ex: ex, tenantId: tenantHeader, method: "PUT", path: $"/customers/{id}", durationMs: sw.ElapsedMilliseconds); return Api.InternalServerError("CUSTOMER_UPDATE_ERROR", "Error al actualizar cliente"); }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/customers")]
        public async Task<IHttpResult> ListCustomers(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var err)) return err!;
            if (page < 1 || pageSize < 1 || pageSize > 100) return Api.BadRequest("VALIDATION_ERROR", "Parámetros de paginación inválidos.");
            try
            {
                var result = await _customerService.ListCustomersAsync(tenantId, page, pageSize);
                Log.Info(context, "customer-list", "Customers listed", tenantId: tenantHeader, method: "GET", path: "/customers", durationMs: sw.ElapsedMilliseconds);
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex) { Log.Error(context, "customer-list", "Error", ex: ex, tenantId: tenantHeader, method: "GET", path: "/customers", durationMs: sw.ElapsedMilliseconds); return Api.InternalServerError("CUSTOMER_LIST_ERROR", "Error al listar clientes"); }
        }
    }
}
