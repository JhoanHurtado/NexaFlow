using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Domain.Entities;

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Tenant(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del negocio es requerido.");
        if (name.Length > 200)
            throw new DomainException("El nombre no puede superar 200 caracteres.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void AssignStripeCustomer(string stripeCustomerId)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
            throw new DomainException("El ID de cliente Stripe es requerido.");
        StripeCustomerId = stripeCustomerId;
    }
}
