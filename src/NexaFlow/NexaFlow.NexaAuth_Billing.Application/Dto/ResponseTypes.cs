namespace NexaFlow.NexaAuth_Billing.Application.Dto;

public record ErrorResponse(string Code, string Message);
public record TenantCreatedResponse(Guid TenantId);
public record UserCreatedResponse(Guid Id);
public record MessageResponse(string Message);
public record WebhookReceivedResponse(bool Received);
public record TenantInfoResponse(Guid Id, string Name);
