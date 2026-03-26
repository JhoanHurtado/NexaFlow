namespace NexaFlow.NexaAuth_Billing.Domain.Events;

public abstract record DomainEvent(Guid TenantId, Guid AggregateId, string AggregateType, DateTime OccurredAt)
{
    public abstract string EventType { get; }
}

public record TenantRegisteredEvent(Guid TenantId, string Name)
    : DomainEvent(TenantId, TenantId, "Tenant", DateTime.UtcNow)
{
    public override string EventType => "tenant.registered";
}

public record UserCreatedEvent(Guid TenantId, Guid UserId, string Email, string Role)
    : DomainEvent(TenantId, UserId, "User", DateTime.UtcNow)
{
    public override string EventType => "user.created";
}

public record SubscriptionUpdatedEvent(Guid TenantId, Guid SubscriptionId, string Status)
    : DomainEvent(TenantId, SubscriptionId, "Subscription", DateTime.UtcNow)
{
    public override string EventType => "subscription.updated";
}
