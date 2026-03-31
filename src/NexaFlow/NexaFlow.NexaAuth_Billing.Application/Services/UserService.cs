using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IAuthLogger _logger;

    public UserService(IUserRepository userRepo, IPasswordHasher hasher, IAuthLogger logger)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(Guid tenantId, CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new DomainException("La contraseña debe tener al menos 8 caracteres.");

        var existing = await _userRepo.GetByEmailAsync(tenantId, request.Email);
        if (existing is not null)
            throw new DomainException("Ya existe un usuario con ese email en este tenant.");

        var hash = _hasher.Hash(request.Password);
        var user = new User(tenantId, request.Name, request.Email, request.Role, hash);
        await _userRepo.SaveAsync(user);

        _logger.Info($"[User] Creado: {user.Email} rol={user.Role} tenant={tenantId}");
        return user.Id;
    }

    public async Task<IEnumerable<UserDto>> ListAsync(Guid tenantId)
    {
        var users = await _userRepo.ListByTenantAsync(tenantId);
        return users.Select(u => new UserDto(u.Id, u.TenantId, u.Name, u.Email, u.Role, u.Active, u.CreatedAt));
    }

    public async Task UpdateRoleAsync(Guid tenantId, Guid userId, string role)
    {
        var user = await _userRepo.GetByIdAsync(tenantId, userId)
            ?? throw new DomainException("Usuario no encontrado.");
        user.UpdateRole(role);
        await _userRepo.UpdateAsync(user);
    }

    public async Task DeactivateAsync(Guid tenantId, Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(tenantId, userId)
            ?? throw new DomainException("Usuario no encontrado.");
        user.Deactivate();
        await _userRepo.UpdateAsync(user);
        _logger.Warning($"[User] Desactivado: {userId} tenant={tenantId}");
    }
}
