namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

public interface IAuthLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
