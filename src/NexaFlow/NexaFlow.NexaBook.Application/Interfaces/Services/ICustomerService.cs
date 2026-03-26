using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Records.Create;

namespace NexaFlow.NexaBook.Application.Interfaces.Services
{
    public interface ICustomerService
    {
        Task<Guid> RegisterAsync(Guid tenantId, CreateCustomerRequest request);
        Task UpdateAsync(Guid tenantId, Guid customerId, UpdateCustomerRequest request);
        Task<ApiResponse<CustomerDTO?>> GetByIdAsync(Guid tenantId, Guid customerId);
        Task<ApiResponse<IEnumerable<CustomerDTO>>> ListAsync(Guid tenantId, int page, int pageSize);
    }
}
