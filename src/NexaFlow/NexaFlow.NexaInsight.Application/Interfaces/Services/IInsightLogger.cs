namespace NexaFlow.NexaInsight.Application.Interfaces.Services;

public interface IInsightLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
