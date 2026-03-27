using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

namespace NexaFlow.NexaAuth_Billing.Handlers;

public class SubscriptionHandler
{
    private readonly ISubscriptionService _subService;
    public SubscriptionHandler(ISubscriptionService subService) => _subService = subService;

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/subscriptions/status")]
    public async Task<IHttpResult> GetStatus(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var sub = await _subService.GetByTenantAsync(tenantId);
            if (sub is null)
            {
                Log.Warn(context, "subscription-status", "Subscription not found",
                    tenantId: tenantHeader, method: "GET", path: "/subscriptions/status");
                return HttpResults.NotFound();
            }
            Log.Info(context, "subscription-status", "Subscription retrieved",
                tenantId: tenantHeader, method: "GET", path: "/subscriptions/status",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.Ok(sub);
        }
        catch (Exception ex)
        {
            Log.Error(context, "subscription-status", "Unhandled error retrieving subscription",
                ex: ex, tenantId: tenantHeader, method: "GET", path: "/subscriptions/status",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError(new { code = "SUBSCRIPTION_ERROR", message = "Error al obtener suscripción" });
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Post, "/webhooks/stripe")]
    public async Task<IHttpResult> StripeWebhook(
        [FromHeader(Name = "stripe-signature")] string signature,
        [FromBody] string rawPayload,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawPayload);
            var eventId   = doc.RootElement.GetProperty("id").GetString()!;
            var eventType = doc.RootElement.GetProperty("type").GetString()!;

            await _subService.HandleWebhookAsync(eventId, eventType, rawPayload);
            Log.Info(context, "webhook-stripe", "Stripe webhook processed",
                method: "POST", path: "/webhooks/stripe",
                durationMs: sw.ElapsedMilliseconds,
                extra: w => { w.WriteString("eventId", eventId); w.WriteString("eventType", eventType); });
            return HttpResults.Ok(new { received = true });
        }
        catch (Exception ex)
        {
            Log.Error(context, "webhook-stripe", "Unhandled error processing Stripe webhook",
                ex: ex, method: "POST", path: "/webhooks/stripe",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError(new { code = "WEBHOOK_ERROR", message = "Error procesando webhook" });
        }
    }
}
