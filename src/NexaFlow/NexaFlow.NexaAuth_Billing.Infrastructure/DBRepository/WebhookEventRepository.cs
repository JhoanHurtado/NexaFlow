using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using Npgsql;

namespace NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly string _conn;
    public WebhookEventRepository(string conn) => _conn = conn;

    public async Task<bool> ExistsAsync(string eventId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM stripe_webhook_events WHERE id = $1)", conn);
        cmd.Parameters.AddWithValue(eventId);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task SaveAsync(string eventId, string eventType, string payload)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO stripe_webhook_events (id, type, payload, processed, created_at)
              VALUES ($1, $2, $3::jsonb, FALSE, NOW())
              ON CONFLICT (id) DO NOTHING", conn);
        cmd.Parameters.AddWithValue(eventId);
        cmd.Parameters.AddWithValue(eventType);
        cmd.Parameters.AddWithValue(payload);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkProcessedAsync(string eventId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE stripe_webhook_events SET processed = TRUE WHERE id = $1", conn);
        cmd.Parameters.AddWithValue(eventId);
        await cmd.ExecuteNonQueryAsync();
    }
}
