using Amazon.Lambda.Core;
using System.Buffers;
using System.Diagnostics;
using System.Text.Json;

namespace NexaFlow.NexaInsight;

public static class Log
{
    public static void Info(ILambdaContext ctx, string function, string message,
        string? tenantId = null, string? method = null, string? path = null,
        long? durationMs = null, Action<Utf8JsonWriter>? extra = null)
        => Write(ctx, "INFO", function, message, tenantId, method, path, durationMs, null, extra);

    public static void Error(ILambdaContext ctx, string function, string message,
        Exception? ex = null, string? tenantId = null, string? method = null, string? path = null,
        long? durationMs = null)
        => Write(ctx, "ERROR", function, message, tenantId, method, path, durationMs, ex, null);

    public static void Warn(ILambdaContext ctx, string function, string message,
        string? tenantId = null, string? method = null, string? path = null)
        => Write(ctx, "WARN", function, message, tenantId, method, path, null, null, null);

    private static void Write(ILambdaContext ctx, string level, string function, string message,
        string? tenantId, string? method, string? path, long? durationMs, Exception? ex,
        Action<Utf8JsonWriter>? extra)
    {
        var buf = new ArrayBufferWriter<byte>(512);
        using var w = new Utf8JsonWriter(buf);

        w.WriteStartObject();
        w.WriteString("timestamp",  DateTime.UtcNow.ToString("o"));
        w.WriteString("level",      level);
        w.WriteString("service",    "insight");
        w.WriteString("function",   function);
        w.WriteString("requestId",  ctx.AwsRequestId);
        w.WriteString("message",    message);

        if (tenantId  != null) w.WriteString("tenantId",   tenantId);
        if (method    != null) w.WriteString("httpMethod",  method);
        if (path      != null) w.WriteString("path",        path);
        if (durationMs.HasValue) w.WriteNumber("durationMs", durationMs.Value);

        if (ex != null)
        {
            w.WriteString("error",      ex.Message);
            w.WriteString("errorType",  ex.GetType().Name);
            if (ex.StackTrace != null)
                w.WriteString("stackTrace", ex.StackTrace);
            if (ex.InnerException != null)
                w.WriteString("innerError", ex.InnerException.Message);
        }

        if (extra != null)
        {
            w.WritePropertyName("extra");
            w.WriteStartObject();
            extra(w);
            w.WriteEndObject();
        }

        w.WriteEndObject();
        w.Flush();

        ctx.Logger.LogInformation(System.Text.Encoding.UTF8.GetString(buf.WrittenSpan));
    }

    public static Stopwatch StartTimer() => Stopwatch.StartNew();
}


