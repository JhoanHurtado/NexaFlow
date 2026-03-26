using Moq;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Application.Services;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;
using NexaFlow.NexaAuth_Billing.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaAuth_Billing.Tests.Application;

public class AuthServiceTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IAuthLogger> _logger = new();

    private AuthService CreateService() =>
        new(_tenantRepo.Object, _userRepo.Object, _hasher.Object, _jwt.Object, _logger.Object);

    [Fact]
    public async Task RegisterTenantAsync_ValidRequest_SavesTenantAndOwner()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

        var request = new RegisterTenantRequest("Mi Barbería", "Juan", "juan@test.com", "password123");
        var tenantId = await CreateService().RegisterTenantAsync(request);

        Assert.NotEqual(Guid.Empty, tenantId);
        _tenantRepo.Verify(r => r.SaveAsync(It.IsAny<Tenant>()), Times.Once);
        _userRepo.Verify(r => r.SaveAsync(It.Is<User>(u => u.Role == "owner")), Times.Once);
    }

    [Fact]
    public async Task RegisterTenantAsync_ShortPassword_ThrowsDomainException()
    {
        var request = new RegisterTenantRequest("Biz", "Name", "a@b.com", "short");
        await Assert.ThrowsAsync<DomainException>(() => CreateService().RegisterTenantAsync(request));
        _tenantRepo.Verify(r => r.SaveAsync(It.IsAny<Tenant>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var user = Build.User(email: "juan@test.com", hash: "hashed");
        _userRepo.Setup(r => r.GetByEmailAsync(Build.TenantId, "juan@test.com")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("pass123", "hashed")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthTokenDto("token", "Bearer", 3600, user.Id, Build.TenantId, "owner"));

        var result = await CreateService().LoginAsync(new LoginRequest(Build.TenantId, "juan@test.com", "pass123"));

        Assert.Equal("token", result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsDomainException()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().LoginAsync(new LoginRequest(Build.TenantId, "x@x.com", "pass")));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsDomainException()
    {
        var user = Build.User();
        _userRepo.Setup(r => r.GetByEmailAsync(Build.TenantId, user.Email)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().LoginAsync(new LoginRequest(Build.TenantId, user.Email, "wrong")));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsDomainException()
    {
        var user = Build.User();
        user.Deactivate();
        _userRepo.Setup(r => r.GetByEmailAsync(Build.TenantId, user.Email)).ReturnsAsync(user);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().LoginAsync(new LoginRequest(Build.TenantId, user.Email, "pass")));
    }
}
