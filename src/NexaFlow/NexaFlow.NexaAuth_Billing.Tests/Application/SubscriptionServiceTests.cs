using Moq;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Services;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaAuth_Billing.Tests.Application;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subRepo = new();
    private readonly Mock<IWebhookEventRepository> _webhookRepo = new();
    private readonly Mock<IAuthLogger> _logger = new();

    private SubscriptionService CreateService() =>
        new(_subRepo.Object, _webhookRepo.Object, _logger.Object);

    [Fact]
    public async Task IsActiveAsync_ActiveSubscription_ReturnsTrue()
    {
        _subRepo.Setup(r => r.GetByTenantAsync(Build.TenantId))
            .ReturnsAsync(Build.Subscription(status: "active"));

        var result = await CreateService().IsActiveAsync(Build.TenantId);
        Assert.True(result);
    }

    [Fact]
    public async Task IsActiveAsync_NoSubscription_ReturnsFalse()
    {
        _subRepo.Setup(r => r.GetByTenantAsync(Build.TenantId))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateService().IsActiveAsync(Build.TenantId);
        Assert.False(result);
    }

    [Fact]
    public async Task HandleWebhookAsync_DuplicateEvent_SkipsProcessing()
    {
        _webhookRepo.Setup(r => r.ExistsAsync("evt_dup")).ReturnsAsync(true);

        await CreateService().HandleWebhookAsync("evt_dup", "customer.subscription.updated", "{}");

        _webhookRepo.Verify(r => r.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhookAsync_UnknownEventType_SavesAndMarksProcessed()
    {
        _webhookRepo.Setup(r => r.ExistsAsync("evt_new")).ReturnsAsync(false);

        await CreateService().HandleWebhookAsync("evt_new", "payment.succeeded", "{}");

        _webhookRepo.Verify(r => r.SaveAsync("evt_new", "payment.succeeded", "{}"), Times.Once);
        _webhookRepo.Verify(r => r.MarkProcessedAsync("evt_new"), Times.Once);
    }
}
