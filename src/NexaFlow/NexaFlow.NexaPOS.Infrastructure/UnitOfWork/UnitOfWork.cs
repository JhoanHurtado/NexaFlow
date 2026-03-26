using Dapper;
using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Events;
using Npgsql;
using System.Text.Json;

namespace NexaFlow.NexaPOS.Infrastructure.UnitOfWork
{
    /// <summary>
    /// Implementación del patrón Unit of Work usando una única <see cref="NpgsqlConnection"/>
    /// y una única <see cref="NpgsqlTransaction"/> compartida entre todas las operaciones.
    /// Garantiza que la venta, el stock y los eventos de dominio se persistan atómicamente
    /// implementando el Outbox Pattern.
    /// <para>
    /// Ciclo de vida esperado:
    /// <code>
    /// await uow.BeginAsync(tenantId);
    /// try {
    ///     await uow.SaveSaleAsync(...);
    ///     await uow.UpdateStockAsync(...);
    ///     await uow.EnqueueEventAsync(...);
    ///     await uow.CommitAsync();
    /// } catch {
    ///     await uow.RollbackAsync();
    ///     throw;
    /// }
    /// </code>
    /// </para>
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _connectionString;
        private NpgsqlConnection? _conn;
        private NpgsqlTransaction? _tx;

        /// <param name="connectionString">Cadena de conexión PostgreSQL.</param>
        public UnitOfWork(string connectionString) => _connectionString = connectionString;

        /// <inheritdoc/>
        public async Task BeginAsync(Guid tenantId)
        {
            _conn = new NpgsqlConnection(_connectionString);
            await _conn.OpenAsync();
            await _conn.ExecuteAsync($"SET app.tenant_id = '{tenantId}'");
            _tx = await _conn.BeginTransactionAsync();
        }

        /// <inheritdoc/>
        public async Task SaveProductAsync(Product product) =>
            await _conn!.ExecuteAsync(
                "INSERT INTO products (id, tenant_id, name, price, active) VALUES (@Id, @TenantId, @Name, @Price, @IsActive)",
                product, _tx);

        /// <inheritdoc/>
        public async Task SaveStockAsync(ProductStock stock) =>
            await _conn!.ExecuteAsync(
                "INSERT INTO product_stock (product_id, tenant_id, quantity, low_stock_threshold) VALUES (@ProductId, @TenantId, @Quantity, @LowStockThreshold)",
                stock, _tx);

        /// <inheritdoc/>
        public async Task SaveSaleAsync(Sale sale) =>
            await _conn!.ExecuteAsync(
                "INSERT INTO sales (id, tenant_id, customer_id, reservation_id, total) VALUES (@Id, @TenantId, @CustomerId, @ReservationId, @Total)",
                sale, _tx);

        /// <inheritdoc/>
        public async Task SaveSaleItemsAsync(IEnumerable<SaleItem> items)
        {
            foreach (var item in items)
                await _conn!.ExecuteAsync(
                    "INSERT INTO sale_items (id, sale_id, product_id, quantity, unit_price) VALUES (@Id, @SaleId, @ProductId, @Quantity, @UnitPrice)",
                    item, _tx);
        }

        /// <inheritdoc/>
        public async Task UpdateStockAsync(ProductStock stock) =>
            await _conn!.ExecuteAsync(
                "UPDATE product_stock SET quantity = @Quantity, updated_at = NOW() WHERE product_id = @ProductId AND tenant_id = @TenantId",
                stock, _tx);

        /// <inheritdoc/>
        public async Task EnqueueEventAsync(DomainEvent domainEvent)
        {
            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            await _conn!.ExecuteAsync(
                "INSERT INTO pos_events (tenant_id, event_type, aggregate_id, aggregate_type, payload, published) VALUES (@TenantId, @EventType, @AggregateId, @AggregateType, @Payload::jsonb, FALSE)",
                new { domainEvent.TenantId, domainEvent.EventType, domainEvent.AggregateId, domainEvent.AggregateType, Payload = payload },
                _tx);
        }

        /// <inheritdoc/>
        public async Task CommitAsync() => await _tx!.CommitAsync();

        /// <inheritdoc/>
        public async Task RollbackAsync() => await _tx!.RollbackAsync();

        /// <summary>Libera la transacción y la conexión de base de datos.</summary>
        public async ValueTask DisposeAsync()
        {
            if (_tx is not null) await _tx.DisposeAsync();
            if (_conn is not null) await _conn.DisposeAsync();
        }

        /// <summary>Libera sincrónicamente. Requerido por el contenedor DI de Lambda.</summary>
        public void Dispose()
        {
            _tx?.Dispose();
            _conn?.Dispose();
        }
    }
}
