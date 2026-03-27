using Amazon.Lambda.Core;
using System.Diagnostics;
using System.Text.Json;

namespace NexaFlow.NexaInsight;

public static class Log
{
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = false };

    public static void Info(ILambdaContext ctx, string function, string message,
        string? tenantId = null, string? method = null, string? path = null,
        long? durationMs = null, object? extra = null)
        => Write(ctx, "INFO", function, message, tenantId, method, path, durationMs, null, extra);

    public static void Error(ILambdaContext ctx, string function, string message,
        Exception? ex = null, string? tenantId = null, string? method = null, string? path = null,
        long? durationMs = null)
        => Write(ctx, "ERROR", function, message, tenantId, method, path, durationMs, ex, null);

    public static void Warn(ILambdaContext ctx, string function, string message,
        string? tenantId = null, string? method = null, string? path = null)
        => Write(ctx, "WARN", function, message, tenantId, method, path, null, null, null);

    private static void Write(ILambdaContext ctx, string level, string function, string message,
        string? tenantId, string? method, string? path, long? durationMs, Exception? ex, object? extra)
    {
        var entry = new Dictionary<string, object?>
        {
            ["timestamp"]  = DateTime.UtcNow.ToString("o"),
            ["level"]      = level,
            ["service"]    = "insight",
            ["function"]   = function,
            ["requestId"]  = ctx.AwsRequestId,
            ["message"]    = message,
        };

        if (tenantId  != null) entry["tenantId"]   = tenantId;
        if (method    != null) entry["httpMethod"]  = method;
        if (path      != null) entry["path"]        = path;
        if (durationMs.HasValue) entry["durationMs"] = durationMs.Value;
        if (ex        != null)
        {
            entry["error"]     = ex.Message;
            entry["errorType"] = ex.GetType().Name;
        }
        if (extra != null) entry["extra"] = extra;

        ctx.Logger.LogInformation(JsonSerializer.Serialize(entry, _opts));
    }

    public static Stopwatch StartTimer() => Stopwatch.StartNew();
}
