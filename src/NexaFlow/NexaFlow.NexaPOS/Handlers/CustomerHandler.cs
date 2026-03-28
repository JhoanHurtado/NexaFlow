using NexaFlow.NexaPOS.Application.Dto;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            try
            {
                var id = await _customerService.CreateAsync(tenantId, body);
                var idStr = id.ToString();
                Log.Info(context, "customer-create", "Customer created",
                    tenantId: tenantHeader, method: "POST", path: "/customers",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => w.WriteString("customerId", idStr));
                return HttpResults.Created($"/customers/{id}", id);
            }
            catch (DomainException ex)
            {
                Log.Warn(context, "customer-create", ex.Message,
                    tenantId: tenantHeader, method: "POST", path: "/customers");
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(context, "customer-create", "Unhandled error creating customer",
                    ex: ex, tenantId: tenantHeader, method: "POST", path: "/customers",
                    durationMs: sw.ElapsedMilliseconds);
                return HttpResults.InternalServerError(new ErrorResponse("CUSTOMER_CREATE_ERROR", "Error al crear cliente"));
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
            var sw = Log.StartTimer();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var validationError))
                return validationError!;
            if (page < 1)    return HttpResults.BadRequest("El parámetro 'page' debe ser mayor o igual a 1.");
            if (pageSize < 1 || pageSize > 100) return HttpResults.BadRequest("El parámetro 'pageSize' debe estar entre 1 y 100.");
            try
            {
                var result = await _customerService.ListCustomersAsync(tenantId, page, pageSize);
                Log.Info(context, "customer-list", "Customers listed",
                    tenantId: tenantHeader, method: "GET", path: "/customers",
                    durationMs: sw.ElapsedMilliseconds,
                    extra: w => { w.WriteNumber("page", page); w.WriteNumber("pageSize", pageSize); });
                return HttpResults.Ok(result);
            }
            catch (DomainException ex)
            {
                Log.Warn(context, "customer-list", ex.Message,
                    tenantId: tenantHeader, method: "GET", path: "/customers");
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(context, "customer-list", "Unhandled error listing customers",
                    ex: ex, tenantId: tenantHeader, method: "GET", path: "/customers",
                    durationMs: sw.ElapsedMilliseconds);
                return HttpResults.InternalServerError(new ErrorResponse("CUSTOMER_LIST_ERROR", "Error al listar clientes"));
            }
        }
    }
}
