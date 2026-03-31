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
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO tenants (id, name, created_at) VALUES ($1, $2, $3)", conn);
        cmd.Parameters.AddWithValue(tenant.Id);
        cmd.Parameters.AddWithValue(tenant.Name);
        cmd.Parameters.AddWithValue(tenant.CreatedAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, stripe_customer_id FROM tenants WHERE id = $1", conn);
        cmd.Parameters.AddWithValue(tenantId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var t = new Tenant(r.GetString(1));
        if (!r.IsDBNull(2)) t.AssignStripeCustomer(r.GetString(2));
        return t;
    }

    public async Task UpdateStripeCustomerAsync(Guid tenantId, string stripeCustomerId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE tenants SET stripe_customer_id = $1 WHERE id = $2", conn);
        cmd.Parameters.AddWithValue(stripeCustomerId);
        cmd.Parameters.AddWithValue(tenantId);
        await cmd.ExecuteNonQueryAsync();
    }
}
