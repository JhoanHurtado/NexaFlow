using NexaFlow.NexaInsight.Application.Dto;

namespace NexaFlow.NexaInsight.Application.Interfaces.Services;

public interface IInsightService
{
    Task<ApiResponse<AverageTicketDto>> GetAverageTicketAsync(Guid tenantId, DateOnly from, DateOnly to);
    Task<ApiResponse<CancellationRateDto>> GetCancellationRateAsync(Guid tenantId, DateOnly from, DateOnly to);
    Task<ApiResponse<IEnumerable<DailySummaryDto>>> GetDailySummaryAsync(Guid tenantId, DateOnly from, DateOnly to);
    Task<ApiResponse<IEnumerable<TopProductDto>>> GetTopProductsAsync(Guid tenantId, DateOnly from, DateOnly to, int limit = 5);
    Task<ApiResponse<IEnumerable<LowStockProductDto>>> GetLowStockProductsAsync(Guid tenantId);
}
