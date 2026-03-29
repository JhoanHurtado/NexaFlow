using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly string _conn;
    public SubscriptionRepository(string conn) => _conn = conn;

    public async Task SaveAsync(Subscription subscription)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO subscriptions
              (id, tenant_id, stripe_subscription_id, stripe_price_id, status,
               current_period_start, current_period_end, cancel_at_period_end, created_at)
              VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9)", conn);
        cmd.Parameters.AddWithValue(subscription.Id);
        cmd.Parameters.AddWithValue(subscription.TenantId);
        cmd.Parameters.AddWithValue(subscription.StripeSubscriptionId);
        cmd.Parameters.AddWithValue((object?)subscription.StripePriceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue(subscription.Status);
        cmd.Parameters.AddWithValue(subscription.CurrentPeriodStart);
        cmd.Parameters.AddWithValue(subscription.CurrentPeriodEnd);
        cmd.Parameters.AddWithValue(subscription.CancelAtPeriodEnd);
        cmd.Parameters.AddWithValue(subscription.CreatedAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Subscription?> GetByTenantAsync(Guid tenantId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT id, tenant_id, stripe_subscription_id, stripe_price_id, status,
                     current_period_start, current_period_end, cancel_at_period_end, created_at
              FROM subscriptions WHERE tenant_id = $1 ORDER BY created_at DESC LIMIT 1", conn);
        cmd.Parameters.AddWithValue(tenantId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapSubscription(r) : null;
    }

    public async Task<Subscription?> GetByStripeIdAsync(string stripeSubscriptionId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT id, tenant_id, stripe_subscription_id, stripe_price_id, status,
                     current_period_start, current_period_end, cancel_at_period_end, created_at
              FROM subscriptions WHERE stripe_subscription_id = $1", conn);
        cmd.Parameters.AddWithValue(stripeSubscriptionId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapSubscription(r) : null;
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"UPDATE subscriptions SET status = $1, current_period_start = $2,
              current_period_end = $3, cancel_at_period_end = $4
              WHERE stripe_subscription_id = $5", conn);
        cmd.Parameters.AddWithValue(subscription.Status);
        cmd.Parameters.AddWithValue(subscription.CurrentPeriodStart);
        cmd.Parameters.AddWithValue(subscription.CurrentPeriodEnd);
        cmd.Parameters.AddWithValue(subscription.CancelAtPeriodEnd);
        cmd.Parameters.AddWithValue(subscription.StripeSubscriptionId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static Subscription MapSubscription(NpgsqlDataReader r) =>
        Subscription.Reconstitute(
            r.GetGuid(0), r.GetGuid(1), r.GetString(2),
            r.IsDBNull(3) ? null : r.GetString(3),
            r.GetString(4), r.GetDateTime(5), r.GetDateTime(6),
            r.GetBoolean(7), r.GetDateTime(8));
}
