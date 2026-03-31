using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Domain.Entities
{
    public class Sale
    {
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public Guid? ReservationId { get; private set; }
        public List<SaleItem> Items { get; private set; } = new();
        public decimal Subtotal { get; private set; }
        public decimal TaxRate { get; private set; }
        public decimal TaxAmount { get; private set; }
        public decimal Total { get; private set; }
        public string Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Sale(Guid tenantId, Guid? customerId = null, Guid? reservationId = null, decimal taxRate = 0)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            CustomerId = customerId;
            ReservationId = reservationId;
            TaxRate = taxRate;
            Subtotal = 0; TaxAmount = 0; Total = 0;
            Status = "pending";
            CreatedAt = DateTime.UtcNow;
        }

        public void AddItem(Product product, int quantity)
        {
            if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor a cero.");
            if (!product.IsActive) throw new DomainException("No se puede vender un producto inactivo.");
            var item = new SaleItem(Id, product.Id, quantity, product.Price);
            Items.Add(item);
            Subtotal += item.UnitPrice * item.Quantity;
            TaxAmount = Math.Round(Subtotal * TaxRate / 100, 2);
            Total = Subtotal + TaxAmount;
        }

        public void Complete() => Status = "completed";
        public void Cancel()   => Status = "cancelled";

        public void ValidateForCheckout()
        {
            if (!Items.Any()) throw new DomainException("La venta debe tener al menos un producto.");
            if (Total <= 0)   throw new DomainException("El total de la venta debe ser mayor a cero.");
        }

        public static Sale Reconstitute(Guid id, Guid tenantId, Guid? customerId, Guid? reservationId,
            decimal subtotal, decimal taxRate, decimal taxAmount, decimal total, string status, DateTime createdAt)
        {
            var s = new Sale(tenantId, customerId, reservationId, taxRate);
            s.Id = id; s.Subtotal = subtotal; s.TaxAmount = taxAmount;
            s.Total = total; s.Status = status; s.CreatedAt = createdAt;
            return s;
        }
    }
}
