using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaInsight.Application.Interfaces.Services;
using NexaFlow.NexaInsight.Domain.Exceptions;

namespace NexaFlow.NexaInsight.Handlers;

public class InsightHandler
{
    private readonly IInsightService _insightService;
    public InsightHandler(IInsightService insightService) => _insightService = insightService;

    /// <summary>GET /insights/average-ticket?from=2024-01-01&amp;to=2024-01-31</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/insights/average-ticket")]
    public async Task<IHttpResult> GetAverageTicket(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to,
        ILambdaContext context)
    {
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var result = await _insightService.GetAverageTicketAsync(
                tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
            return HttpResults.Ok(result);
        }
        catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
        catch (Exception ex)
        {
            context.Logger.LogError($"[InsightHandler.GetAverageTicket] {ex.Message}");
            return HttpResults.InternalServerError("Error al calcular ticket promedio");
        }
    }

    /// <summary>GET /insights/cancellation-rate?from=2024-01-01&amp;to=2024-01-31</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/insights/cancellation-rate")]
    public async Task<IHttpResult> GetCancellationRate(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to,
        ILambdaContext context)
    {
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var result = await _insightService.GetCancellationRateAsync(
                tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
            return HttpResults.Ok(result);
        }
        catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
        catch (Exception ex)
        {
            context.Logger.LogError($"[InsightHandler.GetCancellationRate] {ex.Message}");
            return HttpResults.InternalServerError("Error al calcular tasa de cancelación");
        }
    }

    /// <summary>GET /insights/daily-summary?from=2024-01-01&amp;to=2024-01-31</summary>
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/insights/daily-summary")]
    public async Task<IHttpResult> GetDailySummary(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to,
        ILambdaContext context)
    {
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var result = await _insightService.GetDailySummaryAsync(
                tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
            return HttpResults.Ok(result);
        }
        catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
        catch (Exception ex)
        {
            context.Logger.LogError($"[InsightHandler.GetDailySummary] {ex.Message}");
            return HttpResults.InternalServerError("Error al obtener resumen diario");
        }
    }
}
