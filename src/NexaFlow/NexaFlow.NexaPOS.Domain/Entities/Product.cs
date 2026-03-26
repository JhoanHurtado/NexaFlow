using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Domain.Entities
{
    /// <summary>
    /// Representa un producto del catálogo de un tenant.
    /// Encapsula las reglas de negocio relacionadas con precio y estado del producto.
    /// </summary>
    public class Product
    {
        /// <summary>Identificador único del producto.</summary>
        public Guid Id { get; private set; }

        /// <summary>Identificador del tenant al que pertenece el producto.</summary>
        public Guid TenantId { get; private set; }

        /// <summary>Nombre del producto. Se almacena sin espacios al inicio o final.</summary>
        public string Name { get; private set; }

        /// <summary>Precio unitario del producto al momento de su creación o última actualización.</summary>
        public decimal Price { get; private set; }

        /// <summary>Indica si el producto está disponible para la venta.</summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Crea un nuevo producto con estado activo.
        /// </summary>
        /// <param name="tenantId">Tenant propietario. No puede ser vacío.</param>
        /// <param name="name">Nombre del producto. No puede ser vacío ni superar 200 caracteres.</param>
        /// <param name="price">Precio unitario. No puede ser negativo.</param>
        /// <exception cref="DomainException">Si alguna regla de negocio es violada.</exception>
        public Product(Guid tenantId, string name, decimal price)
        {
            if (tenantId == Guid.Empty)
                throw new DomainException("El tenant es requerido.");
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("El nombre del producto es requerido.");
            if (name.Length > 200)
                throw new DomainException("El nombre no puede superar 200 caracteres.");
            if (price < 0)
                throw new DomainException("El precio no puede ser negativo.");

            Id = Guid.NewGuid();
            TenantId = tenantId;
            Name = name.Trim();
            Price = price;
            IsActive = true;
        }

        /// <summary>
        /// Actualiza el precio del producto.
        /// </summary>
        /// <param name="newPrice">Nuevo precio. No puede ser negativo.</param>
        /// <exception cref="DomainException">Si el precio es negativo.</exception>
        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new DomainException("El precio no puede ser negativo.");

            Price = newPrice;
        }

        /// <summary>
        /// Desactiva el producto impidiendo que sea vendido.
        /// Un producto inactivo no puede ser agregado a una venta.
        /// </summary>
        public void Deactivate() => IsActive = false;
    }
}
