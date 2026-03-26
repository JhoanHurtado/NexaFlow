using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Domain.Entities
{
    /// <summary>
    /// Representa el inventario de un producto para un tenant.
    /// Separado de <see cref="Product"/> para aislar responsabilidades:
    /// <see cref="Product"/> gestiona el catálogo y <see cref="ProductStock"/> gestiona el inventario.
    /// Relación con la BD: tabla <c>product_stock</c>, relación 1 a 1 con <c>products</c>.
    /// </summary>
    public class ProductStock
    {
        /// <summary>Identificador del producto al que pertenece este stock.</summary>
        public Guid ProductId { get; private set; }

        /// <summary>Identificador del tenant propietario.</summary>
        public Guid TenantId { get; private set; }

        /// <summary>Cantidad actual disponible en inventario.</summary>
        public int Quantity { get; private set; }

        /// <summary>
        /// Umbral a partir del cual el stock se considera bajo.
        /// Cuando <see cref="Quantity"/> cae a este valor o menos, se emite un <c>StockLowEvent</c>.
        /// </summary>
        public int LowStockThreshold { get; private set; }

        /// <summary>Fecha y hora de la última modificación del stock.</summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Indica si el stock está en nivel bajo (mayor a 0 pero menor o igual al umbral).
        /// Cuando es <c>true</c> se emite un <c>StockLowEvent</c>.
        /// </summary>
        public bool IsLow => Quantity > 0 && Quantity <= LowStockThreshold;

        /// <summary>
        /// Indica si el stock está completamente agotado.
        /// Cuando es <c>true</c> se emite un <c>StockDepletedEvent</c>.
        /// </summary>
        public bool IsDepleted => Quantity == 0;

        /// <summary>
        /// Crea un nuevo registro de stock para un producto.
        /// </summary>
        /// <param name="productId">ID del producto asociado.</param>
        /// <param name="tenantId">Tenant propietario.</param>
        /// <param name="initialQuantity">Cantidad inicial. No puede ser negativa.</param>
        /// <param name="lowStockThreshold">Umbral de stock bajo. Debe ser al menos 1.</param>
        /// <exception cref="DomainException">Si alguna regla de negocio es violada.</exception>
        public ProductStock(Guid productId, Guid tenantId, int initialQuantity, int lowStockThreshold = 5)
        {
            if (initialQuantity < 0)
                throw new DomainException("El stock inicial no puede ser negativo.");
            if (lowStockThreshold < 1)
                throw new DomainException("El umbral de stock bajo debe ser al menos 1.");

            ProductId = productId;
            TenantId = tenantId;
            Quantity = initialQuantity;
            LowStockThreshold = lowStockThreshold;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Agrega unidades al inventario.
        /// </summary>
        /// <param name="units">Cantidad a agregar. Debe ser mayor a cero.</param>
        /// <exception cref="DomainException">Si las unidades son menores o iguales a cero.</exception>
        public void Add(int units)
        {
            if (units <= 0)
                throw new DomainException("Las unidades a agregar deben ser mayores a cero.");

            Quantity += units;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deduce unidades del inventario. Se llama al procesar una venta.
        /// </summary>
        /// <param name="units">Cantidad a deducir. Debe ser mayor a cero y no superar el stock disponible.</param>
        /// <exception cref="DomainException">Si las unidades son inválidas o superan el stock disponible.</exception>
        public void Deduct(int units)
        {
            if (units <= 0)
                throw new DomainException("Las unidades a deducir deben ser mayores a cero.");
            if (units > Quantity)
                throw new DomainException($"Stock insuficiente. Disponible: {Quantity}, solicitado: {units}.");

            Quantity -= units;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
