using Moq;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Application.Services;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;
using NexaFlow.NexaAuth_Billing.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaAuth_Billing.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAuthLogger> _logger = new();

    private UserService CreateService() => new(_userRepo.Object, _hasher.Object, _logger.Object);

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesUser()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _userRepo.Setup(r => r.GetByEmailAsync(Build.TenantId, "staff@test.com"))
            .ReturnsAsync((User?)null);

        var id = await CreateService().CreateAsync(Build.TenantId,
            new CreateUserRequest("Staff", "staff@test.com", "staff", "password123"));

        Assert.NotEqual(Guid.Empty, id);
        _userRepo.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsDomainException()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(Build.TenantId, "juan@test.com"))
            .ReturnsAsync(Build.User());

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().CreateAsync(Build.TenantId,
                new CreateUserRequest("Juan", "juan@test.com", "staff", "password123")));
    }

    [Fact]
    public async Task CreateAsync_ShortPassword_ThrowsDomainException()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().CreateAsync(Build.TenantId,
                new CreateUserRequest("Juan", "a@b.com", "staff", "short")));
    }

    [Fact]
    public async Task DeactivateAsync_ExistingUser_CallsUpdate()
    {
        var user = Build.User();
        _userRepo.Setup(r => r.GetByIdAsync(Build.TenantId, user.Id)).ReturnsAsync(user);

        await CreateService().DeactivateAsync(Build.TenantId, user.Id);

        _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => !u.Active)), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_UserNotFound_ThrowsDomainException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().DeactivateAsync(Build.TenantId, Guid.NewGuid()));
    }
}
