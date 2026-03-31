using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Application.Interfaces.Repositories
{
    public interface ITenantConfigRepository
    {
        Task<TenantConfig> GetOrDefaultAsync(Guid tenantId);
        Task UpsertAsync(TenantConfig config);
    }
}
