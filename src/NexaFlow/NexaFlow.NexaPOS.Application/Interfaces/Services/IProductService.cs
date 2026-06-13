using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;

namespace NexaFlow.NexaPOS.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<Guid> CreateAsync(Guid tenantId, CreateProductRequest request);
        Task UpdateAsync(Guid tenantId, Guid productId, UpdateProductRequest request);
        Task<ApiResponse<IEnumerable<ProductDTO>>> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }
}
