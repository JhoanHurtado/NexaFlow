using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Events;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly IEventRepository _events;
        private readonly IPosLogger _logger;

        public CustomerService(ICustomerRepository repository, IEventRepository events, IPosLogger logger)
        {
            _repository = repository;
            _events = events;
            _logger = logger;
        }

        public async Task<Guid> CreateAsync(Guid tenantId, CreateCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new DomainException("El nombre del cliente es requerido.");

            _logger.Info($"[Customer] Creando cliente '{request.Name}' para tenant {tenantId}");

            var customer = new Customer(tenantId, request.Name, request.Phone, request.Email);
            await _repository.SaveAsync(customer);

            await _events.PublishAsync(new CustomerCreatedEvent(tenantId, customer.Id, customer.Name));

            _logger.Info($"[Customer] Cliente {customer.Id} creado.");
            return customer.Id;
        }

        public async Task<ApiResponse<IEnumerable<CustomerDTO>>> ListCustomersAsync(Guid tenantId, int page, int pageSize)
        {
            if (page < 1) throw new DomainException("La página debe ser mayor a cero.");
            if (pageSize < 1 || pageSize > 100) throw new DomainException("El tamaño de página debe estar entre 1 y 100.");

            var (customers, total) = await _repository.GetPagedAsync(tenantId, page, pageSize);
            var dtos = customers.Select(c => new CustomerDTO(c.Id, c.Name, c.Phone, c.Email));
            return ApiResponse<IEnumerable<CustomerDTO>>.Ok(dtos, new PaginationMetadata(page, pageSize, total));
        }
    }
}
