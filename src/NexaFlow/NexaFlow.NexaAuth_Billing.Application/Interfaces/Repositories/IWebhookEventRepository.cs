namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;

public interface IWebhookEventRepository
{
    Task<bool> ExistsAsync(string eventId);
    Task SaveAsync(string eventId, string eventType, string payload);
    Task MarkProcessedAsync(string eventId);
}
