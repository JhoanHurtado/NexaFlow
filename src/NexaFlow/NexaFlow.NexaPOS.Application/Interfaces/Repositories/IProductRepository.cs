using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Application.Interfaces.Repositories
{
    /// <summary>
    /// Contrato para operaciones de lectura y escritura de productos.
    /// Las operaciones de escritura dentro de una transacción deben usar <c>IUnitOfWork</c>.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>Persiste un nuevo producto. Usado fuera de transacción atómica.</summary>
        Task SaveAsync(Product product);

        /// <summary>
        /// Obtiene un producto activo por su ID dentro del tenant.
        /// Aplica RLS con <c>SET app.tenant_id</c>.
        /// </summary>
        /// <returns>El producto si existe, <c>null</c> si no se encuentra.</returns>
        Task<Product?> GetByIdAsync(Guid tenantId, Guid productId);

        /// <summary>
        /// Retorna una página de productos activos ordenados por nombre.
        /// Usa window function <c>count(*) OVER()</c> para obtener el total sin segunda consulta.
        /// </summary>
        Task<(IEnumerable<Product> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }
}
