using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;
using NexaFlow.NexaAuth_Billing.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaAuth_Billing.Tests.Domain;

public class SubscriptionTests
{
    [Fact]
    public void Constructor_ValidData_CreatesSubscription()
    {
        var s = Build.Subscription(status: "active");
        Assert.NotEqual(Guid.Empty, s.Id);
        Assert.True(s.IsActive);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("trialing")]
    public void IsActive_ActiveStatuses_ReturnsTrue(string status)
    {
        var s = Build.Subscription(status: status);
        Assert.True(s.IsActive);
    }

    [Theory]
    [InlineData("canceled")]
    [InlineData("past_due")]
    [InlineData("incomplete")]
    public void IsActive_InactiveStatuses_ReturnsFalse(string status)
    {
        var s = Build.Subscription(status: status);
        Assert.False(s.IsActive);
    }

    [Fact]
    public void Constructor_InvalidStatus_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Subscription(Build.TenantId, "sub_x", null, "unknown",
                DateTime.UtcNow, DateTime.UtcNow.AddMonths(1)));
    }

    [Fact]
    public void UpdateStatus_ValidStatus_UpdatesFields()
    {
        var s = Build.Subscription(status: "active");
        var newEnd = DateTime.UtcNow.AddMonths(2);
        s.UpdateStatus("canceled", DateTime.UtcNow, newEnd, false);
        Assert.Equal("canceled", s.Status);
        Assert.False(s.IsActive);
    }
}
