using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Application.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task SaveAsync(Customer customer);
        Task<Customer?> GetByIdAsync(Guid tenantId, Guid customerId);
        Task UpdateAsync(Customer customer);
        Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }
}
