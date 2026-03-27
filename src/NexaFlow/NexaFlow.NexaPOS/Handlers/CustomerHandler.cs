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
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _customerService.CreateAsync(tenantId, body);
                Log.Info(context, "customer-create", "Customer created",
                    tenantId: tenantHeader, method: "POST", path: "/customers",
                    durationMs: sw.ElapsedMilliseconds, extra: new { customerId = id });
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
                return HttpResults.InternalServerError("Error al crear cliente");
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
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _customerService.ListCustomersAsync(tenantId, page, pageSize);
                Log.Info(context, "customer-list", "Customers listed",
                    tenantId: tenantHeader, method: "GET", path: "/customers",
                    durationMs: sw.ElapsedMilliseconds, extra: new { page, pageSize });
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
                return HttpResults.InternalServerError("Error al listar clientes");
            }
        }
    }
}
