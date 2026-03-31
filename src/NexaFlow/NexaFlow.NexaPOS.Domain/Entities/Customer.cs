using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Domain.Entities
{
    /// <summary>
    /// Representa un cliente registrado en el sistema POS de un tenant.
    /// El email se normaliza a minúsculas. El nombre y teléfono se almacenan sin espacios al inicio o final.
    /// </summary>
    public class Customer
    {
        /// <summary>Identificador único del cliente.</summary>
        public Guid Id { get; private set; }

        /// <summary>Tenant al que pertenece el cliente.</summary>
        public Guid TenantId { get; private set; }

        /// <summary>Nombre completo del cliente.</summary>
        public string Name { get; private set; }

        /// <summary>Teléfono de contacto. Opcional. Máximo 20 caracteres.</summary>
        public string? Phone { get; private set; }

        /// <summary>Correo electrónico. Opcional. Se normaliza a minúsculas.</summary>
        public string? Email { get; private set; }

        /// <summary>Fecha y hora UTC de creación del cliente.</summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Crea un nuevo cliente para un tenant.
        /// </summary>
        /// <param name="tenantId">Tenant propietario. No puede ser vacío.</param>
        /// <param name="name">Nombre del cliente. Requerido, máximo 200 caracteres.</param>
        /// <param name="phone">Teléfono. Opcional, máximo 20 caracteres.</param>
        /// <param name="email">Email. Opcional. Debe contener '@' si se proporciona.</param>
        /// <exception cref="DomainException">Si alguna regla de negocio es violada.</exception>
        public Customer(Guid tenantId, string name, string? phone = null, string? email = null)
        {
            if (tenantId == Guid.Empty)
                throw new DomainException("El tenant es requerido.");
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("El nombre del cliente es requerido.");
            if (name.Length > 200)
                throw new DomainException("El nombre no puede superar 200 caracteres.");
            if (email is not null && !email.Contains('@'))
                throw new DomainException("El email no tiene un formato válido.");
            if (phone is not null && phone.Length > 20)
                throw new DomainException("El teléfono no puede superar 20 caracteres.");

            Id = Guid.NewGuid();
            TenantId = tenantId;
            Name = name.Trim();
            Phone = phone?.Trim();
            Email = email?.Trim().ToLowerInvariant();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Reconstituye un cliente desde persistencia, preservando el ID original de la base de datos.
        /// No ejecuta validaciones de negocio.
        /// </summary>
        public static Customer Reconstitute(Guid id, Guid tenantId, string name, string? phone, string? email, DateTime createdAt)
        {
            var c = new Customer(tenantId, name, phone, email);
            c.Id = id;
            c.CreatedAt = createdAt;
            return c;
        }
    }
}
