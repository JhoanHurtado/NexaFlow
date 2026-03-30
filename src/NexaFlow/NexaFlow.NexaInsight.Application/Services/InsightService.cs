using NexaFlow.NexaInsight.Application.Dto;
using NexaFlow.NexaInsight.Application.Interfaces.Repositories;
using NexaFlow.NexaInsight.Application.Interfaces.Services;
using NexaFlow.NexaInsight.Domain.Exceptions;

namespace NexaFlow.NexaInsight.Application.Services;

public class InsightService : IInsightService
{
    private readonly ISalesInsightRepository _salesRepo;
    private readonly IReservationInsightRepository _reservationRepo;
    private readonly IStockInsightRepository _stockRepo;
    private readonly IInsightLogger _logger;

    public InsightService(ISalesInsightRepository salesRepo,
        IReservationInsightRepository reservationRepo,
        IStockInsightRepository stockRepo,
        IInsightLogger logger)
    {
        _salesRepo = salesRepo;
        _reservationRepo = reservationRepo;
        _stockRepo = stockRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<AverageTicketDto>> GetAverageTicketAsync(Guid tenantId, DateOnly from, DateOnly to)
    {
        ValidateRange(from, to);
        var result = await _salesRepo.GetAverageTicketAsync(tenantId, from, to);
        _logger.Info($"[Insight] AvgTicket tenant={tenantId} from={from} to={to} avg={result.Average}");
        return ApiResponse<AverageTicketDto>.Ok(new AverageTicketDto(
            result.TenantId, result.Average, result.Total, result.SaleCount,
            from.ToString(), to.ToString()));
    }

    public async Task<ApiResponse<CancellationRateDto>> GetCancellationRateAsync(Guid tenantId, DateOnly from, DateOnly to)
    {
        ValidateRange(from, to);
        var result = await _reservationRepo.GetCancellationRateAsync(tenantId, from, to);
        _logger.Info($"[Insight] CancellationRate tenant={tenantId} rate={result.RatePercent}%");
        return ApiResponse<CancellationRateDto>.Ok(new CancellationRateDto(
            result.TenantId, result.TotalReservations, result.CancelledReservations,
            result.RatePercent, from.ToString(), to.ToString()));
    }

    public async Task<ApiResponse<IEnumerable<DailySummaryDto>>> GetDailySummaryAsync(Guid tenantId, DateOnly from, DateOnly to)
    {
        ValidateRange(from, to);
        if (to.DayNumber - from.DayNumber > 90)
            throw new DomainException("El rango máximo para el resumen diario es 90 días.");
        var rows = await _salesRepo.GetDailySummaryAsync(tenantId, from, to);
        return ApiResponse<IEnumerable<DailySummaryDto>>.Ok(
            rows.Select(r => new DailySummaryDto(r.Date, r.TotalRevenue, r.SaleCount, r.AverageTicket)));
    }

    public async Task<ApiResponse<IEnumerable<TopProductDto>>> GetTopProductsAsync(Guid tenantId, DateOnly from, DateOnly to, int limit = 5)
    {
        ValidateRange(from, to);
        var rows = await _salesRepo.GetTopProductsAsync(tenantId, from, to, limit);
        return ApiResponse<IEnumerable<TopProductDto>>.Ok(
            rows.Select(r => new TopProductDto(r.ProductId, r.ProductName, r.TotalUnits, r.TotalRevenue)));
    }

    public async Task<ApiResponse<IEnumerable<LowStockProductDto>>> GetLowStockProductsAsync(Guid tenantId)
    {
        var rows = await _stockRepo.GetLowStockProductsAsync(tenantId);
        return ApiResponse<IEnumerable<LowStockProductDto>>.Ok(
            rows.Select(r => new LowStockProductDto(r.ProductId, r.ProductName, r.CurrentStock, r.LowStockThreshold, r.IsDepleted)));
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new DomainException("La fecha de inicio debe ser anterior a la fecha de fin.");
    }
}
