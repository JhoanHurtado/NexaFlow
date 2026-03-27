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
        if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var te)) return te!;
        if (!Validate.TryParseDateOnly(from, "from", out var fromDate, out var fe)) return fe!;
        if (!Validate.TryParseDateOnly(to, "to", out var toDate, out var toe)) return toe!;
        if (fromDate > toDate) return HttpResults.BadRequest("El parámetro 'from' no puede ser posterior a 'to'.");
        try
        {
            var result = await _insightService.GetAverageTicketAsync(tenantId, fromDate, toDate);
            Log.Info(context, "avg-ticket", "Average ticket calculated",
                tenantId: tenantHeader, method: "GET", path: "/insights/average-ticket",
                durationMs: sw.ElapsedMilliseconds,
                extra: w => { w.WriteString("from", from); w.WriteString("to", to); });
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
            return HttpResults.InternalServerError(new { code = "AVG_TICKET_ERROR", message = "Error al calcular ticket promedio" });
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
        if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var te)) return te!;
        if (!Validate.TryParseDateOnly(from, "from", out var fromDate, out var fe)) return fe!;
        if (!Validate.TryParseDateOnly(to, "to", out var toDate, out var toe)) return toe!;
        if (fromDate > toDate) return HttpResults.BadRequest("El parámetro 'from' no puede ser posterior a 'to'.");
        try
        {
            var result = await _insightService.GetCancellationRateAsync(tenantId, fromDate, toDate);
            Log.Info(context, "cancellation-rate", "Cancellation rate calculated",
                tenantId: tenantHeader, method: "GET", path: "/insights/cancellation-rate",
                durationMs: sw.ElapsedMilliseconds,
                extra: w => { w.WriteString("from", from); w.WriteString("to", to); });
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
            return HttpResults.InternalServerError(new { code = "CANCELLATION_RATE_ERROR", message = "Error al calcular tasa de cancelación" });
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
        if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var te)) return te!;
        if (!Validate.TryParseDateOnly(from, "from", out var fromDate, out var fe)) return fe!;
        if (!Validate.TryParseDateOnly(to, "to", out var toDate, out var toe)) return toe!;
        if (fromDate > toDate) return HttpResults.BadRequest("El parámetro 'from' no puede ser posterior a 'to'.");
        try
        {
            var result = await _insightService.GetDailySummaryAsync(tenantId, fromDate, toDate);
            Log.Info(context, "daily-summary", "Daily summary retrieved",
                tenantId: tenantHeader, method: "GET", path: "/insights/daily-summary",
                durationMs: sw.ElapsedMilliseconds,
                extra: w => { w.WriteString("from", from); w.WriteString("to", to); });
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
            return HttpResults.InternalServerError(new { code = "DAILY_SUMMARY_ERROR", message = "Error al obtener resumen diario" });
        }
    }
}
