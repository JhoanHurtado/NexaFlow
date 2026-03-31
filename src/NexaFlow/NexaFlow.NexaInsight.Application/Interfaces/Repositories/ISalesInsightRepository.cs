using NexaFlow.NexaInsight.Domain.Entities;

namespace NexaFlow.NexaInsight.Application.Interfaces.Repositories;

public interface ISalesInsightRepository
{
    Task<AverageTicket> GetAverageTicketAsync(Guid tenantId, DateOnly from, DateOnly to);
    Task<IEnumerable<DailySalesSummary>> GetDailySummaryAsync(Guid tenantId, DateOnly from, DateOnly to);
    Task<IEnumerable<TopProduct>> GetTopProductsAsync(Guid tenantId, DateOnly from, DateOnly to, int limit = 5);
}
