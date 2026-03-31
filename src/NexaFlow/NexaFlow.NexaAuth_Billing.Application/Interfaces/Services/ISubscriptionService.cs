using NexaFlow.NexaAuth_Billing.Application.Dto;

namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetByTenantAsync(Guid tenantId);
    Task<bool> IsActiveAsync(Guid tenantId);
    Task HandleWebhookAsync(string eventId, string eventType, string payload);
}
