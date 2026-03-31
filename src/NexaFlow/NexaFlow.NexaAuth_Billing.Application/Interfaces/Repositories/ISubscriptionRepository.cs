using NexaFlow.NexaAuth_Billing.Domain.Entities;

namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task SaveAsync(Subscription subscription);
    Task<Subscription?> GetByTenantAsync(Guid tenantId);
    Task<Subscription?> GetByStripeIdAsync(string stripeSubscriptionId);
    Task UpdateAsync(Subscription subscription);
}
