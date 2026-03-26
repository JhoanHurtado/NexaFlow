using NexaFlow.NexaBook.Domain.Entities;

namespace NexaFlow.NexaBook.Application.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task SaveAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task<Customer?> GetByIdAsync(Guid tenantId, Guid customerId);
        Task<bool> ExistsByEmailAsync(Guid tenantId, string email);
        Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }
}
