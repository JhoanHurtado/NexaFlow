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
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, customer.TenantId);
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO customers (id, tenant_id, name, phone, email) VALUES ($1, $2, $3, $4, $5)", conn);
            cmd.Parameters.AddWithValue(customer.Id);
            cmd.Parameters.AddWithValue(customer.TenantId);
            cmd.Parameters.AddWithValue(customer.Name);
            cmd.Parameters.AddWithValue((object?)customer.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)customer.Email ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Customer?> GetByIdAsync(Guid tenantId, Guid customerId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                "SELECT id, tenant_id, name, phone, email FROM customers WHERE id = $1 AND tenant_id = $2", conn);
            cmd.Parameters.AddWithValue(customerId);
            cmd.Parameters.AddWithValue(tenantId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new Customer(
                reader.GetGuid(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4));
        }

        public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);
            await using var cmd = new NpgsqlCommand(
                @"SELECT id, tenant_id, name, phone, email, count(*) OVER() AS total_count
                  FROM customers WHERE tenant_id = $1
                  ORDER BY name LIMIT $2 OFFSET $3", conn);
            cmd.Parameters.AddWithValue(tenantId);
            cmd.Parameters.AddWithValue(pageSize);
            cmd.Parameters.AddWithValue((page - 1) * pageSize);

            var items = new List<Customer>();
            int total = 0;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                total = reader.GetInt32(5);
                items.Add(new Customer(
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4)));
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
