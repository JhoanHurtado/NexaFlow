using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Interfaces.Events;
using NexaFlow.NexaBook.Application.Interfaces.Repositories;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Entities;
using NexaFlow.NexaBook.Domain.Events;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly IUnitOfWork _uow;
        private readonly IPosLogger _logger;

        public CustomerService(ICustomerRepository repository, IUnitOfWork uow, IPosLogger logger)
        {
            _repository = repository;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Guid> FindOrCreateAsync(Guid tenantId, CreateCustomerRequest request)
        {
            // Si tiene email, buscar cliente existente primero
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existing = await _repository.GetByEmailAsync(tenantId, request.Email);
                if (existing is not null)
                {
                    _logger.Info($"[Customer] Cliente existente encontrado por email: {existing.Id}");
                    return existing.Id;
                }
            }
            // No existe → registrar
            return await RegisterAsync(tenantId, request);
        }

        public async Task<Guid> RegisterAsync(Guid tenantId, CreateCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new DomainException("El nombre del cliente es requerido.");

            if (request.Email is not null)
            {
                var exists = await _repository.ExistsByEmailAsync(tenantId, request.Email);
                if (exists)
                    throw new DomainException($"Ya existe un cliente con el email '{request.Email}'.");
            }

            _logger.Info($"[Customer] Registrando '{request.Name}' para tenant {tenantId}. SelfRegistered={request.SelfRegistered}");

            var customer = new Customer(tenantId, request.Name, request.Phone, request.Email);

            await _uow.BeginAsync(tenantId);
            try
            {
                await _uow.SaveCustomerAsync(customer);
                await _uow.EnqueueEventAsync(new CustomerRegisteredEvent(tenantId, customer.Id, customer.Name, customer.Email, customer.Phone, request.SelfRegistered));
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.Info($"[Customer] Cliente {customer.Id} registrado.");
            return customer.Id;
        }

        public async Task UpdateAsync(Guid tenantId, Guid customerId, UpdateCustomerRequest request)
        {
            var customer = await _repository.GetByIdAsync(tenantId, customerId)
                ?? throw new DomainException($"Cliente {customerId} no encontrado.");

            if (request.Email is not null && request.Email != customer.Email)
            {
                var exists = await _repository.ExistsByEmailAsync(tenantId, request.Email);
                if (exists)
                    throw new DomainException($"Ya existe un cliente con el email '{request.Email}'.");
            }

            customer.UpdateContact(request.Name, request.Phone, request.Email);

            await _uow.BeginAsync(tenantId);
            try
            {
                await _uow.UpdateCustomerAsync(customer);
                await _uow.EnqueueEventAsync(new CustomerUpdatedEvent(tenantId, customer.Id, customer.Name, customer.Email, customer.Phone));
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.Info($"[Customer] Cliente {customer.Id} actualizado.");
        }

        public async Task<ApiResponse<CustomerDTO?>> GetByIdAsync(Guid tenantId, Guid customerId)
        {
            var customer = await _repository.GetByIdAsync(tenantId, customerId);
            if (customer is null)
            {
                _logger.Warning($"[Customer] Cliente {customerId} no encontrado para tenant {tenantId}.");
                return ApiResponse<CustomerDTO?>.Ok(null);
            }
            return ApiResponse<CustomerDTO?>.Ok(MapToDto(customer));
        }

        public async Task<ApiResponse<IEnumerable<CustomerDTO>>> ListAsync(Guid tenantId, int page, int pageSize)
        {
            if (page < 1) throw new DomainException("La página debe ser mayor a cero.");
            if (pageSize < 1 || pageSize > 100) throw new DomainException("El tamaño de página debe estar entre 1 y 100.");

            var (customers, total) = await _repository.GetPagedAsync(tenantId, page, pageSize);
            return ApiResponse<IEnumerable<CustomerDTO>>.Ok(
                customers.Select(MapToDto),
                new PaginationMetadata(page, pageSize, total));
        }

        private static CustomerDTO MapToDto(Customer c) => new(c.Id, c.Name, c.Phone, c.Email);
    }
}
