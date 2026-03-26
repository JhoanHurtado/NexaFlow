using Dapper;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;

public class UserRepository : IUserRepository
{
    private readonly string _conn;
    public UserRepository(string conn) => _conn = conn;

    public async Task SaveAsync(User user)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{user.TenantId}'");
        await conn.ExecuteAsync(
            @"INSERT INTO users (id, tenant_id, name, email, role, active, created_at)
              VALUES (@Id, @TenantId, @Name, @Email, @Role, @Active, @CreatedAt)",
            new { user.Id, user.TenantId, user.Name, user.Email, user.Role, user.Active, user.CreatedAt });
    }

    public async Task<User?> GetByEmailAsync(Guid tenantId, string email)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM users WHERE tenant_id = @TId AND email = @Email",
            new { TId = tenantId, Email = email.ToLowerInvariant() });
        return row is null ? null : MapUser(row);
    }

    public async Task<User?> GetByIdAsync(Guid tenantId, Guid userId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM users WHERE tenant_id = @TId AND id = @Id",
            new { TId = tenantId, Id = userId });
        return row is null ? null : MapUser(row);
    }

    public async Task<IEnumerable<User>> ListByTenantAsync(Guid tenantId)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
        var rows = await conn.QueryAsync<dynamic>(
            "SELECT * FROM users WHERE tenant_id = @TId ORDER BY created_at",
            new { TId = tenantId });
        var result = new List<User>();
        foreach (var r in rows) result.Add(MapUser(r));
        return result;
    }

    public async Task UpdateAsync(User user)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"SET app.tenant_id = '{user.TenantId}'");
        await conn.ExecuteAsync(
            "UPDATE users SET role = @Role, active = @Active WHERE id = @Id AND tenant_id = @TenantId",
            new { user.Role, user.Active, user.Id, user.TenantId });
    }

    // Users need password_hash column — stored separately from domain entity for security
    private static User MapUser(dynamic r)
    {
        var u = new User((Guid)r.tenant_id, (string)r.name, (string)r.email,
            (string)r.role, (string)r.password_hash);
        if (!(bool)r.active) u.Deactivate();
        return u;
    }
}
