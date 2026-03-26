using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Records.Create;

namespace NexaFlow.NexaPOS.Application.Interfaces.Services
{
    /// <summary>
    /// Contrato del servicio de ventas.
    /// Orquesta la validación de stock, creación de la venta y emisión de eventos en una transacción atómica.
    /// </summary>
    public interface ISaleService
    {
        /// <summary>
        /// Crea una venta validando stock, deduciendo inventario y encolando eventos en una sola transacción.
        /// Emite <c>SaleCreatedEvent</c> y opcionalmente <c>StockLowEvent</c> o <c>StockDepletedEvent</c>.
        /// </summary>
        /// <returns>ID de la venta creada.</returns>
        Task<Guid> CreateAsync(Guid tenantId, CreateSaleRequest request);

        /// <summary>Obtiene una venta con sus ítems por ID.</summary>
        /// <returns>La venta envuelta en <see cref="ApiResponse{T}"/>, o con <c>Data = null</c> si no existe.</returns>
        Task<ApiResponse<SaleDTO?>> GetSaleByIdAsync(Guid tenantId, Guid saleId);

        /// <summary>Retorna una página de ventas del tenant ordenadas por fecha descendente.</summary>
        Task<ApiResponse<IEnumerable<SaleDTO>>> ListSalesAsync(Guid tenantId, int page, int pageSize);
    }

    /// <summary>
    /// Contrato del servicio de clientes.
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Crea un cliente y emite un <c>CustomerCreatedEvent</c>.
        /// </summary>
        /// <returns>ID del cliente creado.</returns>
        Task<Guid> CreateAsync(Guid tenantId, CreateCustomerRequest request);

        /// <summary>Retorna una página de clientes del tenant ordenados por nombre.</summary>
        Task<ApiResponse<IEnumerable<CustomerDTO>>> ListCustomersAsync(Guid tenantId, int page, int pageSize);
    }
}
