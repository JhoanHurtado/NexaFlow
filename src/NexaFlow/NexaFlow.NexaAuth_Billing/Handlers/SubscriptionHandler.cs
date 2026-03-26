using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

namespace NexaFlow.NexaAuth_Billing.Handlers;

public class SubscriptionHandler
{
    private readonly ISubscriptionService _subService;
    public SubscriptionHandler(ISubscriptionService subService) => _subService = subService;

    /// <summary>GET /subscriptions/status — Verifica si el tenant tiene suscripción activa.</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/subscriptions/status")]
    public async Task<IHttpResult> GetStatus(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        ILambdaContext context)
    {
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var sub = await _subService.GetByTenantAsync(tenantId);
            if (sub is null) return HttpResults.NotFound();
            return HttpResults.Ok(sub);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"[SubscriptionHandler.GetStatus] {ex.Message}");
            return HttpResults.InternalServerError("Error al obtener suscripción");
        }
    }

    /// <summary>POST /webhooks/stripe — Recibe eventos de Stripe (idempotente).</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/webhooks/stripe")]
    public async Task<IHttpResult> StripeWebhook(
        [FromHeader(Name = "stripe-signature")] string signature,
        [FromBody] string rawPayload,
        ILambdaContext context)
    {
        try
        {
            // En producción: validar firma con Stripe.net StripeClient.ConstructEvent
            // Aquí parseamos el event id y type del payload directamente
            using var doc = System.Text.Json.JsonDocument.Parse(rawPayload);
            var eventId = doc.RootElement.GetProperty("id").GetString()!;
            var eventType = doc.RootElement.GetProperty("type").GetString()!;

            await _subService.HandleWebhookAsync(eventId, eventType, rawPayload);
            return HttpResults.Ok(new { received = true });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"[SubscriptionHandler.StripeWebhook] {ex.Message}");
            return HttpResults.InternalServerError("Error procesando webhook");
        }
    }
}
