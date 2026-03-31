using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Records;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Application.Services;

public class AuthService : IAuthService
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly IAuthLogger _logger;

    public AuthService(ITenantRepository tenantRepo, IUserRepository userRepo,
        IPasswordHasher hasher, IJwtService jwt, IAuthLogger logger)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
        _hasher = hasher;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<Guid> RegisterTenantAsync(RegisterTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new DomainException("La contraseña debe tener al menos 8 caracteres.");

        var tenant = new Tenant(request.BusinessName);
        await _tenantRepo.SaveAsync(tenant);

        var hash = _hasher.Hash(request.Password);
        var owner = new User(tenant.Id, request.OwnerName, request.OwnerEmail, "owner", hash);
        await _userRepo.SaveAsync(owner);

        _logger.Info($"[Auth] Tenant registrado: {tenant.Id} — owner: {owner.Email}");
        return tenant.Id;
    }

    public async Task<AuthTokenDto> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.TenantId, request.Email)
            ?? throw new DomainException("Credenciales inválidas.");

        if (!user.Active)
            throw new DomainException("El usuario está inactivo.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Credenciales inválidas.");

        _logger.Info($"[Auth] Login exitoso: {user.Email} tenant {request.TenantId}");
        return _jwt.GenerateToken(user.Id, user.TenantId, user.Email, user.Role, user.Name);
    }
}
