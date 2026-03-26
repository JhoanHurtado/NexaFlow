using NexaFlow.NexaAuth_Billing.Domain.Entities;

namespace NexaFlow.NexaAuth_Billing.Tests.Helpers;

internal static class Build
{
    public static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static Tenant Tenant(string name = "Mi Barbería") => new(name);

    public static User User(
        string name = "Juan Perez",
        string email = "juan@test.com",
        string role = "owner",
        string hash = "$2a$12$fakehashfortest",
        Guid? tenantId = null) =>
        new(tenantId ?? TenantId, name, email, role, hash);

    public static Subscription Subscription(
        Guid? tenantId = null,
        string status = "active",
        string stripeSubId = "sub_test123") =>
        new(tenantId ?? TenantId, stripeSubId, "price_test",
            status, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
}
