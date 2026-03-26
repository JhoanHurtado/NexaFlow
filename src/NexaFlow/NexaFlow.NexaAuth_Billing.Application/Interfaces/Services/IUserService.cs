using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Records;

namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

public interface IUserService
{
    Task<Guid> CreateAsync(Guid tenantId, CreateUserRequest request);
    Task<IEnumerable<UserDto>> ListAsync(Guid tenantId);
    Task UpdateRoleAsync(Guid tenantId, Guid userId, string role);
    Task DeactivateAsync(Guid tenantId, Guid userId);
}
