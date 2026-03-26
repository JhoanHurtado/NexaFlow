using NexaFlow.NexaPOS.Domain.Events;

namespace NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork
{
    /// <summary>
    /// Unidad de trabajo que agrupa múltiples operaciones de escritura en una sola transacción de base de datos.
    /// Implementa el Outbox Pattern: la venta, el stock y los eventos se persisten atómicamente.
    /// Si cualquier operación falla, se hace rollback completo garantizando consistencia.
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable, IDisposable
    {
        /// <summary>Abre la conexión, aplica el RLS del tenant e inicia la transacción.</summary>
        Task BeginAsync(Guid tenantId);

        /// <summary>Confirma todos los cambios de la transacción actual.</summary>
        Task CommitAsync();

        /// <summary>Revierte todos los cambios de la transacción actual.</summary>
        Task RollbackAsync();

        /// <summary>Inserta una venta en la tabla <c>sales</c> dentro de la transacción activa.</summary>
        Task SaveSaleAsync(Domain.Entities.Sale sale);

        /// <summary>Inserta los ítems de una venta en la tabla <c>sale_items</c> dentro de la transacción activa.</summary>
        Task SaveSaleItemsAsync(IEnumerable<Domain.Entities.SaleItem> items);

        /// <summary>Inserta un producto en la tabla <c>products</c> dentro de la transacción activa.</summary>
        Task SaveProductAsync(Domain.Entities.Product product);

        /// <summary>Inserta el stock inicial de un producto en <c>product_stock</c> dentro de la transacción activa.</summary>
        Task SaveStockAsync(Domain.Entities.ProductStock stock);

        /// <summary>Actualiza la cantidad de stock en <c>product_stock</c> dentro de la transacción activa.</summary>
        Task UpdateStockAsync(Domain.Entities.ProductStock stock);

        /// <summary>
        /// Inserta un evento de dominio en la tabla <c>pos_events</c> con <c>published = FALSE</c>
        /// dentro de la transacción activa. El evento queda pendiente de ser enviado a SQS/EventBridge.
        /// </summary>
        Task EnqueueEventAsync(DomainEvent domainEvent);
    }
}
