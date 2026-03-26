using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Domain.Entities
{
    /// <summary>
    /// Representa un ítem dentro de una venta.
    /// Captura el precio unitario al momento de la transacción para preservar el historial
    /// aunque el precio del producto cambie posteriormente.
    /// Solo puede ser creado a través de <see cref="Sale.AddItem"/>.
    /// </summary>
    public class SaleItem
    {
        /// <summary>Identificador único del ítem.</summary>
        public Guid Id { get; private set; }

        /// <summary>Venta a la que pertenece este ítem.</summary>
        public Guid SaleId { get; private set; }

        /// <summary>Producto vendido.</summary>
        public Guid ProductId { get; private set; }

        /// <summary>Cantidad vendida.</summary>
        public int Quantity { get; private set; }

        /// <summary>Precio unitario capturado al momento de la venta.</summary>
        public decimal UnitPrice { get; private set; }

        /// <summary>Subtotal calculado: <see cref="UnitPrice"/> × <see cref="Quantity"/>.</summary>
        public decimal Subtotal => UnitPrice * Quantity;

        /// <summary>
        /// Crea un ítem de venta. Solo accesible desde el ensamblado de dominio.
        /// Usar <see cref="Sale.AddItem"/> para crear ítems.
        /// </summary>
        internal SaleItem(Guid saleId, Guid productId, int quantity, decimal unitPrice)
        {
            if (quantity <= 0)
                throw new DomainException("La cantidad debe ser mayor a cero.");
            if (unitPrice < 0)
                throw new DomainException("El precio unitario no puede ser negativo.");

            Id = Guid.NewGuid();
            SaleId = saleId;
            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;
        }
    }
}
