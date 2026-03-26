using System.Text.Json;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Domain.Entities;

namespace NexaFlow.NexaAuth_Billing.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subRepo;
    private readonly IWebhookEventRepository _webhookRepo;
    private readonly IAuthLogger _logger;

    public SubscriptionService(ISubscriptionRepository subRepo,
        IWebhookEventRepository webhookRepo, IAuthLogger logger)
    {
        _subRepo = subRepo;
        _webhookRepo = webhookRepo;
        _logger = logger;
    }

    public async Task<SubscriptionDto?> GetByTenantAsync(Guid tenantId)
    {
        var sub = await _subRepo.GetByTenantAsync(tenantId);
        return sub is null ? null : MapToDto(sub);
    }

    public async Task<bool> IsActiveAsync(Guid tenantId)
    {
        var sub = await _subRepo.GetByTenantAsync(tenantId);
        return sub?.IsActive ?? false;
    }

    public async Task HandleWebhookAsync(string eventId, string eventType, string payload)
    {
        // Idempotencia: ignorar eventos ya procesados
        if (await _webhookRepo.ExistsAsync(eventId))
        {
            _logger.Warning($"[Webhook] Evento duplicado ignorado: {eventId}");
            return;
        }

        await _webhookRepo.SaveAsync(eventId, eventType, payload);

        try
        {
            await ProcessEventAsync(eventType, payload);
            await _webhookRepo.MarkProcessedAsync(eventId);
            _logger.Info($"[Webhook] Procesado: {eventType} id={eventId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"[Webhook] Error procesando {eventType}: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessEventAsync(string eventType, string payload)
    {
        switch (eventType)
        {
            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                break;
            default:
                _logger.Info($"[Webhook] Evento no manejado: {eventType}");
                return;
        }

        // Parsear el objeto data.object del payload de Stripe
        using var doc = JsonDocument.Parse(payload);
        var dataObj = doc.RootElement.GetProperty("data").GetProperty("object");

        if (eventType is "customer.subscription.created" or "customer.subscription.updated")
            await UpsertSubscriptionAsync(dataObj);
        else
            await CancelSubscriptionAsync(dataObj);
    }

    private async Task UpsertSubscriptionAsync(JsonElement obj)
    {
        var stripeSubId = obj.GetProperty("id").GetString()!;
        var status = obj.GetProperty("status").GetString()!;
        var priceId = obj.GetProperty("items").GetProperty("data")[0]
            .GetProperty("price").GetProperty("id").GetString();
        var periodStart = DateTimeOffset.FromUnixTimeSeconds(obj.GetProperty("current_period_start").GetInt64()).UtcDateTime;
        var periodEnd = DateTimeOffset.FromUnixTimeSeconds(obj.GetProperty("current_period_end").GetInt64()).UtcDateTime;
        var cancelAtEnd = obj.GetProperty("cancel_at_period_end").GetBoolean();

        var existing = await _subRepo.GetByStripeIdAsync(stripeSubId);
        if (existing is not null)
        {
            existing.UpdateStatus(status, periodStart, periodEnd, cancelAtEnd);
            await _subRepo.UpdateAsync(existing);
        }
        else
        {
            // Necesitamos el tenant_id — viene en metadata o customer
            // En Stripe, metadata.tenant_id se configura al crear la suscripción
            var tenantIdStr = obj.GetProperty("metadata").GetProperty("tenant_id").GetString()!;
            var tenantId = Guid.Parse(tenantIdStr);
            var sub = new Subscription(tenantId, stripeSubId, priceId, status, periodStart, periodEnd);
            await _subRepo.SaveAsync(sub);
        }
    }

    private async Task CancelSubscriptionAsync(JsonElement obj)
    {
        var stripeSubId = obj.GetProperty("id").GetString()!;
        var existing = await _subRepo.GetByStripeIdAsync(stripeSubId);
        if (existing is null) return;

        var periodStart = DateTimeOffset.FromUnixTimeSeconds(obj.GetProperty("current_period_start").GetInt64()).UtcDateTime;
        var periodEnd = DateTimeOffset.FromUnixTimeSeconds(obj.GetProperty("current_period_end").GetInt64()).UtcDateTime;
        existing.UpdateStatus("canceled", periodStart, periodEnd, false);
        await _subRepo.UpdateAsync(existing);
    }

    private static SubscriptionDto MapToDto(Subscription s) => new(
        s.Id, s.TenantId, s.StripeSubscriptionId, s.StripePriceId,
        s.Status, s.CurrentPeriodStart, s.CurrentPeriodEnd, s.CancelAtPeriodEnd, s.IsActive);
}
