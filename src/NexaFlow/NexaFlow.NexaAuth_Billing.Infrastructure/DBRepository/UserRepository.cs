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
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, user.TenantId);
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO users (id, tenant_id, name, email, role, active, password_hash, created_at) VALUES ($1,$2,$3,$4,$5,$6,$7,$8)", conn);
        cmd.Parameters.AddWithValue(user.Id);
        cmd.Parameters.AddWithValue(user.TenantId);
        cmd.Parameters.AddWithValue(user.Name);
        cmd.Parameters.AddWithValue(user.Email);
        cmd.Parameters.AddWithValue(user.Role);
        cmd.Parameters.AddWithValue(user.Active);
        cmd.Parameters.AddWithValue(user.PasswordHash);
        cmd.Parameters.AddWithValue(user.CreatedAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<User?> GetByEmailAsync(Guid tenantId, string email)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, tenantId);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, tenant_id, name, email, role, active, password_hash FROM users WHERE tenant_id = $1 AND email = $2", conn);
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.AddWithValue(email.ToLowerInvariant());
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapUser(r) : null;
    }

    public async Task<User?> GetByIdAsync(Guid tenantId, Guid userId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, tenantId);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, tenant_id, name, email, role, active, password_hash FROM users WHERE tenant_id = $1 AND id = $2", conn);
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.AddWithValue(userId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapUser(r) : null;
    }

    public async Task<IEnumerable<User>> ListByTenantAsync(Guid tenantId)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, tenantId);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, tenant_id, name, email, role, active, password_hash FROM users WHERE tenant_id = $1 ORDER BY created_at", conn);
        cmd.Parameters.AddWithValue(tenantId);
        await using var r = await cmd.ExecuteReaderAsync();
        var result = new List<User>();
        while (await r.ReadAsync()) result.Add(MapUser(r));
        return result;
    }

    public async Task UpdateAsync(User user)
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await SetTenantAsync(conn, user.TenantId);
        await using var cmd = new NpgsqlCommand(
            "UPDATE users SET role = $1, active = $2 WHERE id = $3 AND tenant_id = $4", conn);
        cmd.Parameters.AddWithValue(user.Role);
        cmd.Parameters.AddWithValue(user.Active);
        cmd.Parameters.AddWithValue(user.Id);
        cmd.Parameters.AddWithValue(user.TenantId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static User MapUser(NpgsqlDataReader r)
    {
        var u = new User(r.GetGuid(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(6));
        if (!r.GetBoolean(5)) u.Deactivate();
        return u;
    }

    private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
    {
        await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
