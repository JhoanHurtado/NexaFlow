namespace NexaFlow.NexaAuth_Billing.Application.Records;

public record RegisterTenantRequest(string BusinessName, string OwnerName, string OwnerEmail, string Password);

public record LoginRequest(Guid TenantId, string Email, string Password);

public record CreateUserRequest(string Name, string Email, string Role, string Password);

public record UpdateUserRoleRequest(string Role);
