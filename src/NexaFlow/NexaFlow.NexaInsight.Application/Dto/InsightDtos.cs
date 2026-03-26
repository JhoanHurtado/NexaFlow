namespace NexaFlow.NexaInsight.Application.Dto;

public record AverageTicketDto(
    Guid TenantId,
    decimal Average,
    decimal TotalRevenue,
    int SaleCount,
    string From,
    string To);

public record CancellationRateDto(
    Guid TenantId,
    int TotalReservations,
    int CancelledReservations,
    decimal RatePercent,
    string From,
    string To);

public record DailySummaryDto(
    DateOnly Date,
    decimal TotalRevenue,
    int SaleCount,
    decimal AverageTicket);
