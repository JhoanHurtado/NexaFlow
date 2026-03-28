using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Events;
using Npgsql;
using System.Text.Json;

namespace NexaFlow.NexaPOS.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _connectionString;
        private NpgsqlConnection? _conn;
        private NpgsqlTransaction? _tx;

        public UnitOfWork(string connectionString) => _connectionString = connectionString;

        public async Task BeginAsync(Guid tenantId)
        {
            _conn = new NpgsqlConnection(_connectionString);
            await _conn.OpenAsync();
            await using var setCmd = new NpgsqlCommand($"SET app.tenant_id = '{tenantId}'", _conn);
            await setCmd.ExecuteNonQueryAsync();
            _tx = await _conn.BeginTransactionAsync();
        }

        public async Task SaveProductAsync(Product product)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO products (id, tenant_id, name, price, active) VALUES ($1, $2, $3, $4, $5)", _conn, _tx);
            cmd.Parameters.AddWithValue(product.Id);
            cmd.Parameters.AddWithValue(product.TenantId);
            cmd.Parameters.AddWithValue(product.Name);
            cmd.Parameters.AddWithValue(product.Price);
            cmd.Parameters.AddWithValue(product.IsActive);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveStockAsync(ProductStock stock)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO product_stock (product_id, tenant_id, quantity, low_stock_threshold) VALUES ($1, $2, $3, $4)", _conn, _tx);
            cmd.Parameters.AddWithValue(stock.ProductId);
            cmd.Parameters.AddWithValue(stock.TenantId);
            cmd.Parameters.AddWithValue(stock.Quantity);
            cmd.Parameters.AddWithValue(stock.LowStockThreshold);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveSaleAsync(Sale sale)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO sales (id, tenant_id, customer_id, reservation_id, total) VALUES ($1, $2, $3, $4, $5)", _conn, _tx);
            cmd.Parameters.AddWithValue(sale.Id);
            cmd.Parameters.AddWithValue(sale.TenantId);
            cmd.Parameters.AddWithValue((object?)sale.CustomerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)sale.ReservationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue(sale.Total);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveSaleItemsAsync(IEnumerable<SaleItem> items)
        {
            foreach (var item in items)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO sale_items (id, sale_id, product_id, quantity, unit_price) VALUES ($1, $2, $3, $4, $5)", _conn, _tx);
                cmd.Parameters.AddWithValue(item.Id);
                cmd.Parameters.AddWithValue(item.SaleId);
                cmd.Parameters.AddWithValue(item.ProductId);
                cmd.Parameters.AddWithValue(item.Quantity);
                cmd.Parameters.AddWithValue(item.UnitPrice);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateStockAsync(ProductStock stock)
        {
            await using var cmd = new NpgsqlCommand(
                "UPDATE product_stock SET quantity = $1, updated_at = NOW() WHERE product_id = $2 AND tenant_id = $3", _conn, _tx);
            cmd.Parameters.AddWithValue(stock.Quantity);
            cmd.Parameters.AddWithValue(stock.ProductId);
            cmd.Parameters.AddWithValue(stock.TenantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EnqueueEventAsync(DomainEvent domainEvent)
        {
            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO pos_events (tenant_id, event_type, aggregate_id, aggregate_type, payload, published)
                  VALUES ($1, $2, $3, $4, $5::jsonb, FALSE)", _conn, _tx);
            cmd.Parameters.AddWithValue(domainEvent.TenantId);
            cmd.Parameters.AddWithValue(domainEvent.EventType);
            cmd.Parameters.AddWithValue(domainEvent.AggregateId);
            cmd.Parameters.AddWithValue(domainEvent.AggregateType);
            cmd.Parameters.AddWithValue(payload);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task CommitAsync() => await _tx!.CommitAsync();
        public async Task RollbackAsync() => await _tx!.RollbackAsync();

        public async ValueTask DisposeAsync()
        {
            if (_tx is not null) await _tx.DisposeAsync();
            if (_conn is not null) await _conn.DisposeAsync();
        }

        public void Dispose()
        {
            _tx?.Dispose();
            _conn?.Dispose();
        }
    }
}
