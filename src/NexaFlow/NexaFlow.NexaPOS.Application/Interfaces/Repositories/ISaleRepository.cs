using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Application.Interfaces.Repositories
{
    /// <summary>
    /// Contrato para operaciones de lectura de ventas.
    /// Las escrituras se realizan a través de <c>IUnitOfWork</c> para garantizar atomicidad.
    /// </summary>
    public interface ISaleRepository
    {
        /// <summary>Persiste una venta con sus ítems. Usado fuera de transacción atómica.</summary>
        Task SaveAsync(Sale sale);

        /// <summary>
        /// Obtiene una venta con sus ítems y nombres de productos por ID.
        /// Aplica RLS con <c>SET app.tenant_id</c>.
        /// </summary>
        /// <returns>La venta con sus ítems, o <c>null</c> si no existe.</returns>
        Task<SaleWithItems?> GetByIdAsync(Guid tenantId, Guid saleId);

        /// <summary>
        /// Retorna una página de ventas ordenadas por fecha descendente.
        /// Carga los ítems de todas las ventas de la página en una sola consulta adicional usando <c>ANY(@Ids)</c>.
        /// </summary>
        Task<(IEnumerable<SaleWithItems> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }

    /// <summary>
    /// Proyección que combina una <see cref="Sale"/> con sus ítems enriquecidos con el nombre del producto.
    /// Evita exponer la entidad de dominio directamente a las capas superiores.
    /// </summary>
    public record SaleWithItems(
        Sale Sale,
        IEnumerable<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> Items);
}
