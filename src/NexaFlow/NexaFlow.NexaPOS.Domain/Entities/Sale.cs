using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Domain.Entities
{
    /// <summary>
    /// Representa una venta realizada por un tenant.
    /// Agrega los ítems vendidos y calcula el total automáticamente.
    /// Puede estar asociada opcionalmente a un cliente y/o una reserva.
    /// </summary>
    public class Sale
    {
        /// <summary>Identificador único de la venta.</summary>
        public Guid Id { get; private set; }

        /// <summary>Tenant al que pertenece la venta.</summary>
        public Guid TenantId { get; private set; }

        /// <summary>Cliente asociado a la venta. Opcional.</summary>
        public Guid? CustomerId { get; private set; }

        /// <summary>
        /// Reserva asociada a la venta. Opcional.
        /// Permite vincular una venta con una reserva previa del sistema NexaBook.
        /// </summary>
        public Guid? ReservationId { get; private set; }

        /// <summary>Lista de ítems que componen la venta.</summary>
        public List<SaleItem> Items { get; private set; } = new();

        /// <summary>Total calculado de la venta. Se actualiza automáticamente al agregar ítems.</summary>
        public decimal Total { get; private set; }

        /// <summary>Fecha y hora UTC en que se creó la venta.</summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Crea una nueva venta vacía para un tenant.
        /// </summary>
        /// <param name="tenantId">Tenant propietario de la venta.</param>
        /// <param name="customerId">Cliente asociado. Opcional.</param>
        /// <param name="reservationId">Reserva asociada. Opcional.</param>
        public Sale(Guid tenantId, Guid? customerId = null, Guid? reservationId = null)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            CustomerId = customerId;
            ReservationId = reservationId;
            CreatedAt = DateTime.UtcNow;
            Total = 0;
        }

        /// <summary>
        /// Agrega un producto a la venta y recalcula el total.
        /// El precio capturado es el precio actual del producto al momento de la venta.
        /// </summary>
        /// <param name="product">Producto a agregar. Debe estar activo.</param>
        /// <param name="quantity">Cantidad. Debe ser mayor a cero.</param>
        /// <exception cref="DomainException">Si la cantidad es inválida o el producto está inactivo.</exception>
        public void AddItem(Product product, int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("La cantidad debe ser mayor a cero.");
            if (!product.IsActive)
                throw new DomainException("No se puede vender un producto inactivo.");

            var item = new SaleItem(Id, product.Id, quantity, product.Price);
            Items.Add(item);
            Total += item.UnitPrice * item.Quantity;
        }

        /// <summary>
        /// Valida que la venta esté lista para ser procesada.
        /// Debe tener al menos un ítem y un total mayor a cero.
        /// </summary>
        /// <exception cref="DomainException">Si la venta no cumple las condiciones mínimas.</exception>
        public void ValidateForCheckout()
        {
            if (!Items.Any())
                throw new DomainException("La venta debe tener al menos un producto.");
            if (Total <= 0)
                throw new DomainException("El total de la venta debe ser mayor a cero.");
        }
    }
}
