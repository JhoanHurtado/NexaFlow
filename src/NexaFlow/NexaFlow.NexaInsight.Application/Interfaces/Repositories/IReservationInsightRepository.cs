using NexaFlow.NexaInsight.Domain.Entities;

namespace NexaFlow.NexaInsight.Application.Interfaces.Repositories;

public interface IReservationInsightRepository
{
    Task<CancellationRate> GetCancellationRateAsync(Guid tenantId, DateOnly from, DateOnly to);
}

public interface IStockInsightRepository
{
    Task<IEnumerable<LowStockProduct>> GetLowStockProductsAsync(Guid tenantId);
}
