using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Tests.Helpers
{
    /// <summary>
    /// Builders para construir entidades de dominio en tests sin repetir código.
    /// </summary>
    internal static class Build
    {
        public static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public static Product Product(
            string name = "Producto Test",
            decimal price = 10.00m,
            Guid? tenantId = null) =>
            new(tenantId ?? TenantId, name, price);

        public static ProductStock Stock(
            Guid? productId = null,
            int quantity = 50,
            int threshold = 5,
            Guid? tenantId = null) =>
            new(productId ?? Guid.NewGuid(), tenantId ?? TenantId, quantity, threshold);

        public static Customer Customer(
            string name = "Cliente Test",
            string? email = "test@test.com",
            string? phone = "1234567890",
            Guid? tenantId = null) =>
            new(tenantId ?? TenantId, name, phone, email);
    }
}
