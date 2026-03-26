using NexaFlow.NexaAuth_Billing.Domain.Entities;

namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task SaveAsync(User user);
    Task<User?> GetByEmailAsync(Guid tenantId, string email);
    Task<User?> GetByIdAsync(Guid tenantId, Guid userId);
    Task<IEnumerable<User>> ListByTenantAsync(Guid tenantId);
    Task UpdateAsync(User user);
}
