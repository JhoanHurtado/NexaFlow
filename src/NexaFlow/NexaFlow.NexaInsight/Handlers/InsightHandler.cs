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

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/insights/average-ticket")]
    public async Task<IHttpResult> GetAverageTicket(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var result = await _insightService.GetAverageTicketAsync(
                tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
            Log.Info(context, "avg-ticket", "Average ticket calculated",
                tenantId: tenantHeader, method: "GET", path: "/insights/average-ticket",
                durationMs: sw.ElapsedMilliseconds, extra: new { from, to });
            return HttpResults.Ok(result);
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "avg-ticket", ex.Message,
                tenantId: tenantHeader, method: "GET", path: "/insights/average-ticket");
            return HttpResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "avg-ticket", "Unhandled error calculating average ticket",
                ex: ex, tenantId: tenantHeader, method: "GET", path: "/insights/average-ticket",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError("Error al calcular ticket promedio");
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/insights/cancellation-rate")]
    public async Task<IHttpResult> GetCancellationRate(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var result = await _insightService.GetCancellationRateAsync(
                tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
            Log.Info(context, "cancellation-rate", "Cancellation rate calculated",
                tenantId: tenantHeader, method: "GET", path: "/insights/cancellation-rate",
                durationMs: sw.ElapsedMilliseconds, extra: new { from, to });
            return HttpResults.Ok(result);
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "cancellation-rate", ex.Message,
                tenantId: tenantHeader, method: "GET", path: "/insights/cancellation-rate");
            return HttpResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "cancellation-rate", "Unhandled error calculating cancellation rate",
                ex: ex, tenantId: tenantHeader, method: "GET", path: "/insights/cancellation-rate",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError("Error al calcular tasa de cancelación");
        }
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/insights/daily-summary")]
    public async Task<IHttpResult> GetDailySummary(
        [FromHeader(Name = "x-tenant-id")] string tenantHeader,
        [FromQuery] string from,
        [FromQuery] string to,
        ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var tenantId = Guid.Parse(tenantHeader);
            var result = await _insightService.GetDailySummaryAsync(
                tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
            Log.Info(context, "daily-summary", "Daily summary retrieved",
                tenantId: tenantHeader, method: "GET", path: "/insights/daily-summary",
                durationMs: sw.ElapsedMilliseconds, extra: new { from, to });
            return HttpResults.Ok(result);
        }
        catch (DomainException ex)
        {
            Log.Warn(context, "daily-summary", ex.Message,
                tenantId: tenantHeader, method: "GET", path: "/insights/daily-summary");
            return HttpResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(context, "daily-summary", "Unhandled error retrieving daily summary",
                ex: ex, tenantId: tenantHeader, method: "GET", path: "/insights/daily-summary",
                durationMs: sw.ElapsedMilliseconds);
            return HttpResults.InternalServerError("Error al obtener resumen diario");
        }
    }
}
