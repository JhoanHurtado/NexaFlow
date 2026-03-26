using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;
using NexaFlow.NexaAuth_Billing.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaAuth_Billing.Tests.Domain;

public class TenantTests
{
    [Fact]
    public void Constructor_ValidName_CreatesTenant()
    {
        var t = Build.Tenant("Mi Barbería");
        Assert.NotEqual(Guid.Empty, t.Id);
        Assert.Equal("Mi Barbería", t.Name);
        Assert.Null(t.StripeCustomerId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => new Tenant(name));
    }

    [Fact]
    public void Constructor_NameTooLong_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Tenant(new string('x', 201)));
    }

    [Fact]
    public void AssignStripeCustomer_ValidId_SetsId()
    {
        var t = Build.Tenant();
        t.AssignStripeCustomer("cus_abc123");
        Assert.Equal("cus_abc123", t.StripeCustomerId);
    }

    [Fact]
    public void AssignStripeCustomer_Empty_ThrowsDomainException()
    {
        var t = Build.Tenant();
        Assert.Throws<DomainException>(() => t.AssignStripeCustomer(""));
    }
}
