namespace NexaFlow.NexaPOS.Domain.Entities
{
    public class TenantConfig
    {
        public Guid TenantId { get; private set; }
        public decimal TaxRate { get; private set; }
        public string Currency { get; private set; }
        public int SlotDurationMinutes { get; private set; }
        public TimeOnly OpenTime { get; private set; }
        public TimeOnly CloseTime { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public TenantConfig(Guid tenantId, decimal taxRate = 19m, string currency = "COP",
            int slotDurationMinutes = 60, TimeOnly? openTime = null, TimeOnly? closeTime = null)
        {
            TenantId = tenantId;
            TaxRate = taxRate;
            Currency = currency;
            SlotDurationMinutes = slotDurationMinutes;
            OpenTime  = openTime  ?? new TimeOnly(8, 0);
            CloseTime = closeTime ?? new TimeOnly(20, 0);
            UpdatedAt = DateTime.UtcNow;
        }

        public static TenantConfig Default(Guid tenantId) =>
            new(tenantId, 19m, "COP", 60, new TimeOnly(8, 0), new TimeOnly(20, 0));

        public static TenantConfig Reconstitute(Guid tenantId, decimal taxRate, string currency,
            int slotDurationMinutes, TimeOnly openTime, TimeOnly closeTime, DateTime updatedAt)
        {
            var c = new TenantConfig(tenantId, taxRate, currency, slotDurationMinutes, openTime, closeTime);
            c.UpdatedAt = updatedAt;
            return c;
        }
    }
}
