using NexaFlow.NexaBook.Application.Interfaces.Repositories;
using NexaFlow.NexaBook.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaBook.Infrastructure.DBRepository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;
        public CustomerRepository(string connectionString) => _connectionString = connectionString;

        public async Task SaveAsync(Customer customer)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, customer.TenantId);
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO customers (id, tenant_id, name, phone, email) VALUES ($1,$2,$3,$4,$5)", conn);
            cmd.Parameters.AddWithValue(customer.Id);
            cmd.Parameters.AddWithValue(customer.TenantId);
            cmd.Parameters.AddWithValue(customer.Name);
            cmd.Parameters.AddWithValue((object?)customer.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)customer.Email ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, customer.TenantId);
            await using var cmd = new NpgsqlCommand(
                "UPDATE customers SET name=$1, phone=$2, email=$3 WHERE id=$4 AND tenant_id=$5", conn);
            cmd.Parameters.AddWithValue(customer.Name);
            cmd.Parameters.AddWithValue((object?)customer.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)customer.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue(customer.Id);
            cmd.Parameters.AddWithValue(customer.TenantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Customer?> GetByIdAsync(Guid tenantId, Guid customerId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT id,tenant_id,name,phone,email,created_at FROM customers WHERE id=$1 AND tenant_id=$2", conn);
            cmd.Parameters.AddWithValue(customerId);
            cmd.Parameters.AddWithValue(tenantId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            return Customer.Reconstitute(r.GetGuid(0), r.GetGuid(1), r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4),
                r.GetDateTime(5));
        }

        public async Task<bool> ExistsByEmailAsync(Guid tenantId, string email)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM customers WHERE tenant_id=$1 AND email=$2)", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(email.ToLowerInvariant());
            return (bool)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<Customer?> GetByEmailAsync(Guid tenantId, string email)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT id,tenant_id,name,phone,email,created_at FROM customers WHERE tenant_id=$1 AND email=$2", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(email.ToLowerInvariant());
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            return Customer.Reconstitute(r.GetGuid(0), r.GetGuid(1), r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4),
                r.GetDateTime(5));
        }        public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                @"SELECT id,tenant_id,name,phone,email,created_at,count(*) OVER() AS total
                  FROM customers WHERE tenant_id=$1 ORDER BY name LIMIT $2 OFFSET $3", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(pageSize);
            cmd.Parameters.AddWithValue((page - 1) * pageSize);
            var items = new List<Customer>();
            int total = 0;
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                total = (int)r.GetInt64(6);
                items.Add(Customer.Reconstitute(r.GetGuid(0), r.GetGuid(1), r.GetString(2),
                    r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4),
                    r.GetDateTime(5)));
            }
            return (items, total);
        }

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await using var cmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
