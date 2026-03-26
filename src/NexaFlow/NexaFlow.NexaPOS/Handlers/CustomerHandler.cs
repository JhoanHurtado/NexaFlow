using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Handlers
{
    /// <summary>
    /// Handler Lambda para operaciones sobre clientes.
    /// Expone los endpoints <c>POST /customers</c> y <c>GET /customers</c> vía API Gateway REST.
    /// Requiere el header <c>x-tenant-id</c> en todos los requests.
    /// </summary>
    public class CustomerHandler
    {
        private readonly ICustomerService _customerService;

        /// <param name="customerService">Servicio de clientes inyectado por DI.</param>
        public CustomerHandler(ICustomerService customerService) => _customerService = customerService;

        /// <summary>
        /// Registra un nuevo cliente para el tenant.
        /// Retorna 201 Created con el ID del cliente.
        /// Retorna 400 si el nombre está vacío, el email es inválido o el teléfono supera 20 caracteres.
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/customers")]
        public async Task<IHttpResult> CreateCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateCustomerRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _customerService.CreateAsync(tenantId, body);
                return HttpResults.Created($"/customers/{id}", id);
            }
            catch (DomainException ex)
            {
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.CreateCustomer] {ex.Message}");
                return HttpResults.InternalServerError("Error al crear cliente");
            }
        }

        /// <summary>
        /// Lista los clientes del tenant con paginación, ordenados por nombre.
        /// Retorna 200 con <c>ApiResponse&lt;IEnumerable&lt;CustomerDTO&gt;&gt;</c>.
        /// </summary>
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
                var result = await _customerService.ListCustomersAsync(tenantId, page, pageSize);
                return HttpResults.Ok(result);
            }
            catch (DomainException ex)
            {
                return HttpResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[CustomerHandler.ListCustomers] {ex.Message}");
                return HttpResults.InternalServerError("Error al listar clientes");
            }
        }
    }
}
