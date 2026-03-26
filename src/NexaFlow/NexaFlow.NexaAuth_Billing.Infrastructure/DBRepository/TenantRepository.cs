using Dapper;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;

public class TenantRepository : ITenantRepository
{
    private readonly string _conn;
    public TenantRepository(string conn) => _conn = conn;

    public async Task SaveAsync(Tenant tenant)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            "INSERT INTO tenants (id, name, created_at) VALUES (@Id, @Name, @CreatedAt)",
            new { tenant.Id, tenant.Name, tenant.CreatedAt });
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT id, name, stripe_customer_id, created_at FROM tenants WHERE id = @Id",
            new { Id = tenantId });
        if (row is null) return null;
        var t = new Tenant((string)row.name);
        if (row.stripe_customer_id is not null)
            t.AssignStripeCustomer((string)row.stripe_customer_id);
        return t;
    }

    public async Task UpdateStripeCustomerAsync(Guid tenantId, string stripeCustomerId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            "UPDATE tenants SET stripe_customer_id = @StripeId WHERE id = @Id",
            new { StripeId = stripeCustomerId, Id = tenantId });
    }
}
