using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Exceptions;
using NexaFlow.NexaPOS.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaPOS.Tests.Domain
{
    public class ProductTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesProduct()
        {
            var product = Build.Product("Coca Cola", 1.50m);

            Assert.NotEqual(Guid.Empty, product.Id);
            Assert.Equal("Coca Cola", product.Name);
            Assert.Equal(1.50m, product.Price);
            Assert.True(product.IsActive);
            Assert.Equal(Build.TenantId, product.TenantId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_EmptyName_ThrowsDomainException(string name)
        {
            Assert.Throws<DomainException>(() => Build.Product(name));
        }

        [Fact]
        public void Constructor_NegativePrice_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => Build.Product(price: -1m));
        }

        [Fact]
        public void Constructor_EmptyTenantId_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => new Product(Guid.Empty, "Test", 10m));
        }

        [Fact]
        public void Constructor_NameExceeds200Chars_ThrowsDomainException()
        {
            var longName = new string('A', 201);
            Assert.Throws<DomainException>(() => Build.Product(longName));
        }

        [Fact]
        public void UpdatePrice_ValidPrice_UpdatesPrice()
        {
            var product = Build.Product(price: 10m);
            product.UpdatePrice(20m);
            Assert.Equal(20m, product.Price);
        }

        [Fact]
        public void UpdatePrice_NegativePrice_ThrowsDomainException()
        {
            var product = Build.Product();
            Assert.Throws<DomainException>(() => product.UpdatePrice(-5m));
        }

        [Fact]
        public void Deactivate_ActiveProduct_SetsIsActiveFalse()
        {
            var product = Build.Product();
            product.Deactivate();
            Assert.False(product.IsActive);
        }
    }
}
