using NexaFlow.NexaPOS.Application.Dto;

namespace NexaFlow.NexaPOS.Application.Interfaces.Services
{
    public interface ITenantConfigService
    {
        Task<ApiResponse<TenantConfigDTO>> GetAsync(Guid tenantId);
        Task<ApiResponse<TenantConfigDTO>> UpdateAsync(Guid tenantId, UpdateTenantConfigRequest request);
    }
}
