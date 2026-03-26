using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;
using NexaFlow.NexaAuth_Billing.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaAuth_Billing.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_ValidData_CreatesUser()
    {
        var u = Build.User();
        Assert.NotEqual(Guid.Empty, u.Id);
        Assert.True(u.Active);
        Assert.Equal("owner", u.Role);
        Assert.Equal("juan@test.com", u.Email);
    }

    [Theory]
    [InlineData("superadmin")]
    [InlineData("viewer")]
    public void Constructor_InvalidRole_ThrowsDomainException(string role)
    {
        Assert.Throws<DomainException>(() => Build.User(role: role));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("")]
    public void Constructor_InvalidEmail_ThrowsDomainException(string email)
    {
        Assert.Throws<DomainException>(() => Build.User(email: email));
    }

    [Fact]
    public void Constructor_EmptyTenant_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new User(Guid.Empty, "Name", "a@b.com", "owner", "hash"));
    }

    [Fact]
    public void Deactivate_SetsActiveFalse()
    {
        var u = Build.User();
        u.Deactivate();
        Assert.False(u.Active);
    }

    [Fact]
    public void UpdateRole_ValidRole_ChangesRole()
    {
        var u = Build.User(role: "owner");
        u.UpdateRole("admin");
        Assert.Equal("admin", u.Role);
    }

    [Fact]
    public void UpdateRole_InvalidRole_ThrowsDomainException()
    {
        var u = Build.User();
        Assert.Throws<DomainException>(() => u.UpdateRole("superadmin"));
    }
}
