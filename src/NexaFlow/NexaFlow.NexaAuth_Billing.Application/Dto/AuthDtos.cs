namespace NexaFlow.NexaAuth_Billing.Application.Dto;

public record TenantDto(Guid Id, string Name, string? StripeCustomerId, DateTime CreatedAt);

public record UserDto(Guid Id, Guid TenantId, string Name, string Email, string Role, bool Active, DateTime CreatedAt);

public record SubscriptionDto(
    Guid Id, Guid TenantId, string StripeSubscriptionId, string? StripePriceId,
    string Status, DateTime CurrentPeriodStart, DateTime CurrentPeriodEnd,
    bool CancelAtPeriodEnd, bool IsActive);

public record AuthTokenDto(string AccessToken, string TokenType, int ExpiresIn, Guid UserId, Guid TenantId, string Role);
