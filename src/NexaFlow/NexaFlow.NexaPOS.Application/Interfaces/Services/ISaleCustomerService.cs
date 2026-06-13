using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;

namespace NexaFlow.NexaPOS.Application.Interfaces.Services
{
    public interface ISaleService
    {
        Task<Guid> CreateAsync(Guid tenantId, CreateSaleRequest request);
        Task<ApiResponse<SaleDTO?>> GetSaleByIdAsync(Guid tenantId, Guid saleId);
        Task<ApiResponse<IEnumerable<SaleDTO>>> ListSalesAsync(Guid tenantId, int page, int pageSize);
        Task UpdateStatusAsync(Guid tenantId, Guid saleId, string status);
    }

    public interface ICustomerService
    {
        Task<Guid> CreateAsync(Guid tenantId, CreateCustomerRequest request);
        Task UpdateAsync(Guid tenantId, Guid customerId, UpdateCustomerRequest request);
        Task<ApiResponse<IEnumerable<CustomerDTO>>> ListCustomersAsync(Guid tenantId, int page, int pageSize);
    }
}
