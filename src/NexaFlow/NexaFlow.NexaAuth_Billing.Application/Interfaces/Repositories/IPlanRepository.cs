namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;

public interface IPlanRepository
{
    Task<IEnumerable<PlanRecord>> GetAllAsync();
}

public record PlanRecord(string Id, string Name, decimal Price, int MaxUsers, string? StripePriceId);
