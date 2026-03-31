using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Domain.Entities
{
    /// <summary>
    /// Representa un cliente del sistema NexaBook.
    /// Un cliente puede registrarse por sí mismo o ser registrado por el staff del tenant.
    /// El email se normaliza a minúsculas y es único por tenant.
    /// </summary>
    public class Customer
    {
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public string Name { get; private set; }
        public string? Phone { get; private set; }
        public string? Email { get; private set; }
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Crea un nuevo cliente para un tenant.
        /// </summary>
        /// <param name="tenantId">Tenant propietario. No puede ser vacío.</param>
        /// <param name="name">Nombre completo. Requerido, máximo 200 caracteres.</param>
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
        /// Actualiza los datos de contacto del cliente.
        /// </summary>
        public void UpdateContact(string name, string? phone, string? email)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("El nombre del cliente es requerido.");
            if (name.Length > 200)
                throw new DomainException("El nombre no puede superar 200 caracteres.");
            if (email is not null && !email.Contains('@'))
                throw new DomainException("El email no tiene un formato válido.");
            if (phone is not null && phone.Length > 20)
                throw new DomainException("El teléfono no puede superar 20 caracteres.");

            Name = name.Trim();
            Phone = phone?.Trim();
            Email = email?.Trim().ToLowerInvariant();
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
