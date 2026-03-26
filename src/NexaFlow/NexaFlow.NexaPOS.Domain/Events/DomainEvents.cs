namespace NexaFlow.NexaPOS.Domain.Events
{
    /// <summary>
    /// Clase base para todos los eventos de dominio del sistema POS.
    /// Los eventos se persisten en la tabla <c>pos_events</c> con <c>published = FALSE</c>
    /// dentro de la misma transacción que origina el evento (Outbox Pattern).
    /// Un proceso separado los lee y los envía a SQS/EventBridge.
    /// </summary>
    public abstract record DomainEvent(Guid TenantId, Guid AggregateId, string AggregateType, DateTime OccurredAt)
    {
        /// <summary>Tipo del evento. Se usa como discriminador en la tabla <c>pos_events</c>.</summary>
        public abstract string EventType { get; }
    }

    /// <summary>
    /// Emitido cuando una venta es completada exitosamente.
    /// Incluye el total, cantidad de ítems y referencias opcionales a cliente y reserva.
    /// </summary>
    public record SaleCreatedEvent(Guid TenantId, Guid SaleId, decimal Total, int ItemCount, Guid? CustomerId, Guid? ReservationId)
        : DomainEvent(TenantId, SaleId, "Sale", DateTime.UtcNow)
    {
        /// <inheritdoc/>
        public override string EventType => "sale.created";
    }

    /// <summary>
    /// Emitido cuando un producto es creado en el catálogo junto con su stock inicial.
    /// </summary>
    public record ProductCreatedEvent(Guid TenantId, Guid ProductId, string Name, decimal Price)
        : DomainEvent(TenantId, ProductId, "Product", DateTime.UtcNow)
    {
        /// <inheritdoc/>
        public override string EventType => "product.created";
    }

    /// <summary>
    /// Emitido cuando un producto es desactivado y ya no puede ser vendido.
    /// </summary>
    public record ProductDeactivatedEvent(Guid TenantId, Guid ProductId, string Name)
        : DomainEvent(TenantId, ProductId, "Product", DateTime.UtcNow)
    {
        /// <inheritdoc/>
        public override string EventType => "product.deactivated";
    }

    /// <summary>
    /// Emitido cuando el stock de un producto es modificado manualmente (entrada o ajuste).
    /// </summary>
    public record StockUpdatedEvent(Guid TenantId, Guid ProductId, string ProductName, int NewQuantity, int Delta)
        : DomainEvent(TenantId, ProductId, "Product", DateTime.UtcNow)
    {
        /// <inheritdoc/>
        public override string EventType => "stock.updated";
    }

    /// <summary>
    /// Emitido cuando el stock de un producto cae por debajo del umbral configurado
    /// (<see cref="Domain.Entities.ProductStock.LowStockThreshold"/>) pero aún es mayor a cero.
    /// </summary>
    public record StockLowEvent(Guid TenantId, Guid ProductId, string ProductName, int CurrentQuantity, int Threshold)
        : DomainEvent(TenantId, ProductId, "Product", DateTime.UtcNow)
    {
        /// <inheritdoc/>
        public override string EventType => "stock.low";
    }

    /// <summary>
    /// Emitido cuando el stock de un producto llega a cero tras una venta.
    /// Indica que el producto no puede ser vendido hasta que se reponga el inventario.
    /// </summary>
    public record StockDepletedEvent(Guid TenantId, Guid ProductId, string ProductName)
        : DomainEvent(TenantId, ProductId, "Product", DateTime.UtcNow)
    {
        /// <inheritdoc/>
        public override string EventType => "stock.depleted";
    }

    /// <summary>
    /// Emitido cuando un nuevo cliente es registrado en el sistema.
    /// </summary>
    public record CustomerCreatedEvent(Guid TenantId, Guid CustomerId, string Name)
        : DomainEvent(TenantId, CustomerId, "Customer", DateTime.UtcNow)
    {
        /// <inheritdoc/>
        public override string EventType => "customer.created";
    }
}
