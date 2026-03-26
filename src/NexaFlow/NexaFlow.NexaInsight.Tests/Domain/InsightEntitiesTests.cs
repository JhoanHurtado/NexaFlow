using NexaFlow.NexaInsight.Domain.Entities;
using NexaFlow.NexaInsight.Domain.Exceptions;
using NexaFlow.NexaInsight.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaInsight.Tests.Domain;

public class InsightEntitiesTests
{
    [Fact]
    public void AverageTicket_ZeroSales_AverageIsZero()
    {
        var at = new AverageTicket(Build.TenantId, 0m, 0m, 0, Build.From, Build.To);
        Assert.Equal(0m, at.Average);
        Assert.Equal(0, at.SaleCount);
    }

    [Fact]
    public void CancellationRate_AllCancelled_RateIs100()
    {
        var cr = new CancellationRate(Build.TenantId, 10, 10, 100m, Build.From, Build.To);
        Assert.Equal(100m, cr.RatePercent);
    }

    [Fact]
    public void CancellationRate_NoCancellations_RateIsZero()
    {
        var cr = new CancellationRate(Build.TenantId, 10, 0, 0m, Build.From, Build.To);
        Assert.Equal(0m, cr.RatePercent);
    }
}
