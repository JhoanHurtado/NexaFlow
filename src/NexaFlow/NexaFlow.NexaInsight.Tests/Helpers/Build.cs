using NexaFlow.NexaInsight.Domain.Entities;

namespace NexaFlow.NexaInsight.Tests.Helpers;

internal static class Build
{
    public static readonly Guid TenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly DateOnly From = new(2024, 1, 1);
    public static readonly DateOnly To = new(2024, 1, 31);

    public static AverageTicket AverageTicket(decimal average = 50m, int count = 10) =>
        new(TenantId, average, average * count, count, From, To);

    public static CancellationRate CancellationRate(int total = 20, int cancelled = 4) =>
        new(TenantId, total, cancelled, cancelled * 100m / total, From, To);
}
