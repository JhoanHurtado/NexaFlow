using Dapper;
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
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, customer.TenantId);
            await conn.ExecuteAsync(
                "INSERT INTO customers (id, tenant_id, name, phone, email) VALUES (@Id, @TenantId, @Name, @Phone, @Email)",
                customer);
        }

        public async Task UpdateAsync(Customer customer)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, customer.TenantId);
            await conn.ExecuteAsync(
                "UPDATE customers SET name = @Name, phone = @Phone, email = @Email WHERE id = @Id AND tenant_id = @TenantId",
                customer);
        }

        public async Task<Customer?> GetByIdAsync(Guid tenantId, Guid customerId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM customers WHERE id = @Id AND tenant_id = @TenantId",
                new { Id = customerId, TenantId = tenantId });
            return row is null ? null : Map(row);
        }

        public async Task<bool> ExistsByEmailAsync(Guid tenantId, string email)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            return await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM customers WHERE tenant_id = @TenantId AND email = @Email)",
                new { TenantId = tenantId, Email = email.ToLowerInvariant() });
        }

        public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var result = await conn.QueryAsync<dynamic>(
                @"SELECT *, count(*) OVER() AS TotalCount FROM customers
                  WHERE tenant_id = @TId ORDER BY name LIMIT @Limit OFFSET @Offset",
                new { TId = tenantId, Limit = pageSize, Offset = (page - 1) * pageSize });

            var list = result.ToList();
            var total = list.Count > 0 ? (int)list[0].totalcount : 0;
            return (list.Select(r => Map(r)).Cast<Customer>().ToList(), total);
        }

        private static Customer Map(dynamic r) =>
            new((Guid)r.tenant_id, (string)r.name, (string?)r.phone, (string?)r.email);

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
        }
    }
}
