using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Exceptions;
using NexaFlow.NexaPOS.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaPOS.Tests.Domain
{
    public class CustomerTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesCustomer()
        {
            var customer = Build.Customer("Juan Pérez", "juan@test.com", "3001234567");

            Assert.NotEqual(Guid.Empty, customer.Id);
            Assert.Equal("Juan Pérez", customer.Name);
            Assert.Equal("juan@test.com", customer.Email);
            Assert.Equal("3001234567", customer.Phone);
            Assert.Equal(Build.TenantId, customer.TenantId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_EmptyName_ThrowsDomainException(string name)
        {
            Assert.Throws<DomainException>(() => Build.Customer(name));
        }

        [Fact]
        public void Constructor_EmptyTenantId_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => new Customer(Guid.Empty, "Test"));
        }

        [Fact]
        public void Constructor_InvalidEmail_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => Build.Customer(email: "not-an-email"));
        }

        [Fact]
        public void Constructor_NullEmail_IsAllowed()
        {
            var customer = Build.Customer(email: null);
            Assert.Null(customer.Email);
        }

        [Fact]
        public void Constructor_NullPhone_IsAllowed()
        {
            var customer = Build.Customer(phone: null);
            Assert.Null(customer.Phone);
        }

        [Fact]
        public void Constructor_PhoneTooLong_ThrowsDomainException()
        {
            var longPhone = new string('1', 21);
            Assert.Throws<DomainException>(() => Build.Customer(phone: longPhone));
        }

        [Fact]
        public void Constructor_EmailIsNormalized_ToLowercase()
        {
            var customer = Build.Customer(email: "TEST@EXAMPLE.COM");
            Assert.Equal("test@example.com", customer.Email);
        }

        [Fact]
        public void Constructor_NameIsTrimmed()
        {
            var customer = Build.Customer("  Juan  ");
            Assert.Equal("Juan", customer.Name);
        }
    }
}
