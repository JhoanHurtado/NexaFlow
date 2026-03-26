using NexaFlow.NexaInsight.Application.Interfaces.Services;

namespace NexaFlow.NexaInsight.Infrastructura.Logging;

public class LambdaInsightLogger : IInsightLogger
{
    public void Info(string message) => Console.WriteLine($"[INFO] {message}");
    public void Warning(string message) => Console.WriteLine($"[WARN] {message}");
    public void Error(string message) => Console.Error.WriteLine($"[ERROR] {message}");
}
