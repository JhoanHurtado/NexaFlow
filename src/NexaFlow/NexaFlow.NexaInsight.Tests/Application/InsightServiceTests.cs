using Moq;
using NexaFlow.NexaInsight.Application.Interfaces.Repositories;
using NexaFlow.NexaInsight.Application.Interfaces.Services;
using NexaFlow.NexaInsight.Application.Services;
using NexaFlow.NexaInsight.Domain.Entities;
using NexaFlow.NexaInsight.Domain.Exceptions;
using NexaFlow.NexaInsight.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaInsight.Tests.Application;

public class InsightServiceTests
{
    private readonly Mock<ISalesInsightRepository> _salesRepo = new();
    private readonly Mock<IReservationInsightRepository> _reservationRepo = new();
    private readonly Mock<IInsightLogger> _logger = new();

    private InsightService CreateService() =>
        new(_salesRepo.Object, _reservationRepo.Object, _logger.Object);

    [Fact]
    public async Task GetAverageTicketAsync_ValidRange_ReturnsDto()
    {
        _salesRepo.Setup(r => r.GetAverageTicketAsync(Build.TenantId, Build.From, Build.To))
            .ReturnsAsync(Build.AverageTicket(average: 75m, count: 8));

        var result = await CreateService().GetAverageTicketAsync(Build.TenantId, Build.From, Build.To);

        Assert.Equal(75m, result.Average);
        Assert.Equal(8, result.SaleCount);
        Assert.Equal(Build.TenantId, result.TenantId);
    }

    [Fact]
    public async Task GetCancellationRateAsync_ValidRange_ReturnsDto()
    {
        _reservationRepo.Setup(r => r.GetCancellationRateAsync(Build.TenantId, Build.From, Build.To))
            .ReturnsAsync(Build.CancellationRate(total: 20, cancelled: 5));

        var result = await CreateService().GetCancellationRateAsync(Build.TenantId, Build.From, Build.To);

        Assert.Equal(20, result.TotalReservations);
        Assert.Equal(5, result.CancelledReservations);
        Assert.Equal(25m, result.RatePercent);
    }

    [Fact]
    public async Task GetAverageTicketAsync_FromAfterTo_ThrowsDomainException()
    {
        var from = new DateOnly(2024, 2, 1);
        var to = new DateOnly(2024, 1, 1);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().GetAverageTicketAsync(Build.TenantId, from, to));
    }

    [Fact]
    public async Task GetDailySummaryAsync_RangeOver90Days_ThrowsDomainException()
    {
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 5, 1); // > 90 days

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().GetDailySummaryAsync(Build.TenantId, from, to));
    }

    [Fact]
    public async Task GetDailySummaryAsync_ValidRange_ReturnsMappedDtos()
    {
        var summaries = new[]
        {
            new DailySalesSummary(Build.TenantId, new DateOnly(2024, 1, 1), 300m, 6, 50m),
            new DailySalesSummary(Build.TenantId, new DateOnly(2024, 1, 2), 150m, 3, 50m)
        };
        _salesRepo.Setup(r => r.GetDailySummaryAsync(Build.TenantId, Build.From, Build.To))
            .ReturnsAsync(summaries);

        var result = (await CreateService().GetDailySummaryAsync(Build.TenantId, Build.From, Build.To)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(300m, result[0].TotalRevenue);
        Assert.Equal(50m, result[1].AverageTicket);
    }
}
