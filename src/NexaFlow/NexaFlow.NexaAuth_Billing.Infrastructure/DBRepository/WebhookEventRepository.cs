using Dapper;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using Npgsql;

namespace NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly string _conn;
    public WebhookEventRepository(string conn) => _conn = conn;

    public async Task<bool> ExistsAsync(string eventId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM stripe_webhook_events WHERE id = @Id)",
            new { Id = eventId });
    }

    public async Task SaveAsync(string eventId, string eventType, string payload)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            @"INSERT INTO stripe_webhook_events (id, type, payload, processed, created_at)
              VALUES (@Id, @Type, @Payload::jsonb, FALSE, NOW())
              ON CONFLICT (id) DO NOTHING",
            new { Id = eventId, Type = eventType, Payload = payload });
    }

    public async Task MarkProcessedAsync(string eventId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            "UPDATE stripe_webhook_events SET processed = TRUE WHERE id = @Id",
            new { Id = eventId });
    }
}
