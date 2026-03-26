using Dapper;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Domain.Entities;
using Npgsql;

namespace NexaFlow.NexaPOS.Infrastructure.DBRepository
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

        public async Task<Customer?> GetByIdAsync(Guid tenantId, Guid customerId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM customers WHERE id = @Id AND tenant_id = @TenantId",
                new { Id = customerId, TenantId = tenantId });
            if (row is null) return null;
            return new Customer((Guid)row.tenant_id, (string)row.name, (string?)row.phone, (string?)row.email);
        }

        public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await SetTenantAsync(conn, tenantId);
            var result = await conn.QueryAsync<dynamic>(
                @"SELECT *, count(*) OVER() as TotalCount FROM customers
                  WHERE tenant_id = @TId ORDER BY name LIMIT @Limit OFFSET @Offset",
                new { TId = tenantId, Limit = pageSize, Offset = (page - 1) * pageSize });

            var items = result.Select(r => new Customer((Guid)r.tenant_id, (string)r.name, (string?)r.phone, (string?)r.email));
            var total = result.FirstOrDefault()?.totalcount ?? 0;
            return (items, (int)total);
        }

        private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
        }
    }
}
