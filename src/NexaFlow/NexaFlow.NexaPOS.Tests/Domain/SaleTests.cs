using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Exceptions;
using NexaFlow.NexaPOS.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaPOS.Tests.Domain
{
    public class SaleTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesSale()
        {
            var sale = new Sale(Build.TenantId);

            Assert.NotEqual(Guid.Empty, sale.Id);
            Assert.Equal(Build.TenantId, sale.TenantId);
            Assert.Equal(0m, sale.Total);
            Assert.Empty(sale.Items);
            Assert.Null(sale.CustomerId);
            Assert.Null(sale.ReservationId);
        }

        [Fact]
        public void Constructor_WithCustomerAndReservation_SetsOptionalFields()
        {
            var customerId = Guid.NewGuid();
            var reservationId = Guid.NewGuid();
            var sale = new Sale(Build.TenantId, customerId, reservationId);

            Assert.Equal(customerId, sale.CustomerId);
            Assert.Equal(reservationId, sale.ReservationId);
        }

        [Fact]
        public void AddItem_ValidProduct_AddsItemAndCalculatesTotal()
        {
            var sale = new Sale(Build.TenantId);
            var product = Build.Product(price: 10m);

            sale.AddItem(product, 3);

            Assert.Single(sale.Items);
            Assert.Equal(30m, sale.Total);
        }

        [Fact]
        public void AddItem_MultipleProducts_AccumulatesTotal()
        {
            var sale = new Sale(Build.TenantId);
            sale.AddItem(Build.Product(price: 10m), 2);
            sale.AddItem(Build.Product(price: 5m), 4);

            Assert.Equal(2, sale.Items.Count);
            Assert.Equal(40m, sale.Total);
        }

        [Fact]
        public void AddItem_ZeroQuantity_ThrowsDomainException()
        {
            var sale = new Sale(Build.TenantId);
            Assert.Throws<DomainException>(() => sale.AddItem(Build.Product(), 0));
        }

        [Fact]
        public void AddItem_NegativeQuantity_ThrowsDomainException()
        {
            var sale = new Sale(Build.TenantId);
            Assert.Throws<DomainException>(() => sale.AddItem(Build.Product(), -1));
        }

        [Fact]
        public void AddItem_InactiveProduct_ThrowsDomainException()
        {
            var sale = new Sale(Build.TenantId);
            var product = Build.Product();
            product.Deactivate();

            var ex = Assert.Throws<DomainException>(() => sale.AddItem(product, 1));
            Assert.Contains("inactivo", ex.Message);
        }

        [Fact]
        public void ValidateForCheckout_WithItems_DoesNotThrow()
        {
            var sale = new Sale(Build.TenantId);
            sale.AddItem(Build.Product(price: 10m), 1);

            var ex = Record.Exception(() => sale.ValidateForCheckout());
            Assert.Null(ex);
        }

        [Fact]
        public void ValidateForCheckout_NoItems_ThrowsDomainException()
        {
            var sale = new Sale(Build.TenantId);
            Assert.Throws<DomainException>(() => sale.ValidateForCheckout());
        }
    }
}
