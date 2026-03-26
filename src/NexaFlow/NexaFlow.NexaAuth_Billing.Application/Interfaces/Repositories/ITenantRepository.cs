using NexaFlow.NexaAuth_Billing.Domain.Entities;

namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;

public interface ITenantRepository
{
    Task SaveAsync(Tenant tenant);
    Task<Tenant?> GetByIdAsync(Guid tenantId);
    Task UpdateStripeCustomerAsync(Guid tenantId, string stripeCustomerId);
}
