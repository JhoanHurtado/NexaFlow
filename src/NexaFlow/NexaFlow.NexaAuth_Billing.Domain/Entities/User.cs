using NexaFlow.NexaAuth_Billing.Domain.Exceptions;

namespace NexaFlow.NexaAuth_Billing.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Role { get; private set; }
    public string PasswordHash { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private static readonly string[] ValidRoles = ["owner", "admin", "staff"];

    public User(Guid tenantId, string name, string email, string role, string passwordHash)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException("El tenant es requerido.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre es requerido.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("El email es inválido.");
        if (!ValidRoles.Contains(role))
            throw new DomainException("El rol debe ser owner, admin o staff.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("El hash de contraseña es requerido.");

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        PasswordHash = passwordHash;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => Active = false;
    public void UpdateRole(string role)
    {
        if (!ValidRoles.Contains(role))
            throw new DomainException("Rol inválido.");
        Role = role;
    }
}
