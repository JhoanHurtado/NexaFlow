using NexaFlow.NexaInsight.Application.Dto;

namespace NexaFlow.NexaInsight.Application.Interfaces.Services;

public interface IInsightService
{
    Task<AverageTicketDto> GetAverageTicketAsync(Guid tenantId, DateOnly from, DateOnly to);
    Task<CancellationRateDto> GetCancellationRateAsync(Guid tenantId, DateOnly from, DateOnly to);
    Task<IEnumerable<DailySummaryDto>> GetDailySummaryAsync(Guid tenantId, DateOnly from, DateOnly to);
}
