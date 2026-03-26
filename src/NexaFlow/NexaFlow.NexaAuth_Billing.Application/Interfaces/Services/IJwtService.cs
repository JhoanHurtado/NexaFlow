using NexaFlow.NexaAuth_Billing.Application.Dto;

namespace NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

public interface IJwtService
{
    AuthTokenDto GenerateToken(Guid userId, Guid tenantId, string email, string role);
}
