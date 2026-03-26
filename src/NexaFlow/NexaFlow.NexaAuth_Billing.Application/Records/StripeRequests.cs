namespace NexaFlow.NexaAuth_Billing.Application.Records;

public record StripeWebhookRequest(string EventId, string EventType, string Payload);
