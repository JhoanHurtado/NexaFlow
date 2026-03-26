using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Application.Interfaces.Repositories
{
    /// <summary>
    /// Contrato para operaciones de lectura y escritura de clientes.
    /// </summary>
    public interface ICustomerRepository
    {
        /// <summary>Persiste un nuevo cliente en la tabla <c>customers</c>.</summary>
        Task SaveAsync(Customer customer);

        /// <summary>
        /// Obtiene un cliente por su ID dentro del tenant.
        /// </summary>
        /// <returns>El cliente si existe, <c>null</c> si no se encuentra.</returns>
        Task<Customer?> GetByIdAsync(Guid tenantId, Guid customerId);

        /// <summary>Retorna una página de clientes ordenados por nombre.</summary>
        Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }
}
