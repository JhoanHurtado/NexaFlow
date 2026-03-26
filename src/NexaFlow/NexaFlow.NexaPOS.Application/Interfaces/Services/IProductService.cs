using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Records.Create;

namespace NexaFlow.NexaPOS.Application.Interfaces.Services
{
    /// <summary>
    /// Contrato del servicio de productos.
    /// Orquesta la creación atómica de producto + stock + evento usando <c>IUnitOfWork</c>.
    /// </summary>
    public interface IProductService
    {
        /// <summary>Crea un producto con su stock inicial en una sola transacción.</summary>
        Task<Guid> CreateAsync(Guid tenantId, CreateProductRequest request);

        /// <summary>Retorna una página de productos activos del tenant.</summary>
        Task<ApiResponse<IEnumerable<ProductDTO>>> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }
}
