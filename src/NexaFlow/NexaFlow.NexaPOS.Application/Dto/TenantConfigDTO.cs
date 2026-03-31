namespace NexaFlow.NexaPOS.Application.Dto
{
    public record TenantConfigDTO(
        Guid TenantId,
        decimal TaxRate,
        string Currency,
        int SlotDurationMinutes,
        string OpenTime,
        string CloseTime,
        DateTime UpdatedAt);

    public record UpdateTenantConfigRequest(
        decimal TaxRate,
        string Currency,
        int SlotDurationMinutes,
        string OpenTime,
        string CloseTime);
}
