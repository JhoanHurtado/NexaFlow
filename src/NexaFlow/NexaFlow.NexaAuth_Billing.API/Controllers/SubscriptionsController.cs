using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

namespace NexaFlow.NexaAuth_Billing.API.Controllers;

[ApiController]
[Route("subscriptions")]
[Produces("application/json")]
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpGet("status")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatus([FromHeader(Name = "x-tenant-id")] string? tenantHeader)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var sub = await subscriptionService.GetByTenantAsync(tenantId);
            if (sub is null)
                return NotFound(ApiResponse<object>.Fail("SUBSCRIPTION_NOT_FOUND", "Suscripción no encontrada"));
            return Ok(sub);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("SUBSCRIPTION_ERROR", "Error al obtener suscripción"));
        }
    }

    [HttpPost("/webhooks/stripe")]
    [ProducesResponseType(typeof(WebhookReceivedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StripeWebhook(
        [FromHeader(Name = "stripe-signature")] string signature,
        [FromBody] string rawPayload)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawPayload);
            var eventId   = doc.RootElement.GetProperty("id").GetString()!;
            var eventType = doc.RootElement.GetProperty("type").GetString()!;
            await subscriptionService.HandleWebhookAsync(eventId, eventType, rawPayload);
            return Ok(new WebhookReceivedResponse(true));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("WEBHOOK_ERROR", "Error procesando webhook"));
        }
    }
}
