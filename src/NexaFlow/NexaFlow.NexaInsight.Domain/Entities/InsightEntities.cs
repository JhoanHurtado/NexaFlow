namespace NexaFlow.NexaInsight.Domain.Entities;

/// <summary>
/// Resultado del cálculo de ticket promedio para un tenant en un rango de fechas.
/// </summary>
public record AverageTicket(
    Guid TenantId,
    decimal Average,
    decimal Total,
    int SaleCount,
    DateOnly From,
    DateOnly To);

/// <summary>
/// Resultado del análisis de tasa de cancelación de reservas.
/// </summary>
public record CancellationRate(
    Guid TenantId,
    int TotalReservations,
    int CancelledReservations,
    decimal RatePercent,
    DateOnly From,
    DateOnly To);

/// <summary>
/// KPI diario precalculado: ventas totales y cantidad de transacciones.
/// </summary>
public record DailySalesSummary(
    Guid TenantId,
    DateOnly Date,
    decimal TotalRevenue,
    int SaleCount,
    decimal AverageTicket);
