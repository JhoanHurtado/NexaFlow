using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Domain.Entities;

public class Subscription
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string StripeSubscriptionId { get; private set; }
    public string? StripePriceId { get; private set; }
    public string Status { get; private set; }
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private static readonly string[] ValidStatuses = ["trialing", "active", "past_due", "canceled", "incomplete"];

    public Subscription(Guid tenantId, string stripeSubscriptionId, string? stripePriceId,
        string status, DateTime periodStart, DateTime periodEnd)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException("El tenant es requerido.");
        if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            throw new DomainException("El ID de suscripción Stripe es requerido.");
        if (!ValidStatuses.Contains(status))
            throw new DomainException($"Estado inválido: {status}.");

        Id = Guid.NewGuid();
        TenantId = tenantId;
        StripeSubscriptionId = stripeSubscriptionId;
        StripePriceId = stripePriceId;
        Status = status;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsActive => Status is "active" or "trialing";

    public void UpdateStatus(string status, DateTime periodStart, DateTime periodEnd, bool cancelAtPeriodEnd)
    {
        if (!ValidStatuses.Contains(status))
            throw new DomainException($"Estado inválido: {status}.");
        Status = status;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        CancelAtPeriodEnd = cancelAtPeriodEnd;
    }

    /// <summary>
    /// Reconstituye una suscripción desde persistencia, preservando el ID original de la base de datos.
    /// No ejecuta validaciones de negocio.
    /// </summary>
    public static Subscription Reconstitute(Guid id, Guid tenantId, string stripeSubscriptionId, string? stripePriceId,
        string status, DateTime periodStart, DateTime periodEnd, bool cancelAtPeriodEnd, DateTime createdAt)
    {
        var s = new Subscription(tenantId, stripeSubscriptionId, stripePriceId, status, periodStart, periodEnd);
        s.Id = id;
        s.CancelAtPeriodEnd = cancelAtPeriodEnd;
        s.CreatedAt = createdAt;
        return s;
    }
}
