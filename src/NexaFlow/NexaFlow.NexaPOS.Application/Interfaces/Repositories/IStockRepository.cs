using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Application.Interfaces.Repositories
{
    /// <summary>
    /// Contrato para operaciones de lectura y escritura de stock de productos.
    /// Las actualizaciones dentro de una venta deben hacerse a través de <c>IUnitOfWork</c>
    /// para garantizar atomicidad con la transacción de la venta.
    /// </summary>
    public interface IStockRepository
    {
        /// <summary>
        /// Obtiene el stock de un producto dentro del tenant.
        /// </summary>
        /// <returns>El stock si existe, <c>null</c> si no se ha registrado stock para el producto.</returns>
        Task<ProductStock?> GetByProductIdAsync(Guid tenantId, Guid productId);

        /// <summary>Inserta el registro de stock inicial de un producto.</summary>
        Task SaveAsync(ProductStock stock);

        /// <summary>Actualiza la cantidad de stock de un producto fuera de transacción.</summary>
        Task UpdateAsync(ProductStock stock);
    }
}
