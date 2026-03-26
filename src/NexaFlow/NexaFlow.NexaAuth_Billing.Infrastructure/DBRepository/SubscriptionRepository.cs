using Dapper;
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
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            @"INSERT INTO subscriptions
              (id, tenant_id, stripe_subscription_id, stripe_price_id, status,
               current_period_start, current_period_end, cancel_at_period_end, created_at)
              VALUES (@Id, @TenantId, @StripeSubscriptionId, @StripePriceId, @Status,
                      @CurrentPeriodStart, @CurrentPeriodEnd, @CancelAtPeriodEnd, @CreatedAt)",
            new
            {
                subscription.Id, subscription.TenantId, subscription.StripeSubscriptionId,
                subscription.StripePriceId, subscription.Status,
                subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd,
                subscription.CancelAtPeriodEnd, subscription.CreatedAt
            });
    }

    public async Task<Subscription?> GetByTenantAsync(Guid tenantId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM subscriptions WHERE tenant_id = @TId ORDER BY created_at DESC LIMIT 1",
            new { TId = tenantId });
        return row is null ? null : MapSubscription(row);
    }

    public async Task<Subscription?> GetByStripeIdAsync(string stripeSubscriptionId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM subscriptions WHERE stripe_subscription_id = @StripeId",
            new { StripeId = stripeSubscriptionId });
        return row is null ? null : MapSubscription(row);
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            @"UPDATE subscriptions SET status = @Status,
              current_period_start = @CurrentPeriodStart,
              current_period_end = @CurrentPeriodEnd,
              cancel_at_period_end = @CancelAtPeriodEnd
              WHERE stripe_subscription_id = @StripeSubscriptionId",
            new
            {
                subscription.Status, subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd, subscription.CancelAtPeriodEnd,
                subscription.StripeSubscriptionId
            });
    }

    private static Subscription MapSubscription(dynamic r) =>
        new((Guid)r.tenant_id, (string)r.stripe_subscription_id, (string?)r.stripe_price_id,
            (string)r.status, (DateTime)r.current_period_start, (DateTime)r.current_period_end);
}
