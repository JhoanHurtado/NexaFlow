using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Records;

namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Guid> RegisterTenantAsync(RegisterTenantRequest request);
    Task<AuthTokenDto> LoginAsync(LoginRequest request);
}
