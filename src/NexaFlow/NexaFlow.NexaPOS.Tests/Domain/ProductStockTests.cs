using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Exceptions;
using NexaFlow.NexaPOS.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaPOS.Tests.Domain
{
    public class ProductStockTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesStock()
        {
            var stock = Build.Stock(quantity: 100, threshold: 10);

            Assert.Equal(100, stock.Quantity);
            Assert.Equal(10, stock.LowStockThreshold);
            Assert.False(stock.IsLow);
            Assert.False(stock.IsDepleted);
        }

        [Fact]
        public void Constructor_NegativeQuantity_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => Build.Stock(quantity: -1));
        }

        [Fact]
        public void Constructor_ZeroThreshold_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => Build.Stock(threshold: 0));
        }

        [Fact]
        public void Deduct_SufficientStock_ReducesQuantity()
        {
            var stock = Build.Stock(quantity: 20);
            stock.Deduct(5);
            Assert.Equal(15, stock.Quantity);
        }

        [Fact]
        public void Deduct_ExactStock_LeavesDepleted()
        {
            var stock = Build.Stock(quantity: 5);
            stock.Deduct(5);
            Assert.Equal(0, stock.Quantity);
            Assert.True(stock.IsDepleted);
        }

        [Fact]
        public void Deduct_InsufficientStock_ThrowsDomainException()
        {
            var stock = Build.Stock(quantity: 3);
            var ex = Assert.Throws<DomainException>(() => stock.Deduct(5));
            Assert.Contains("Stock insuficiente", ex.Message);
        }

        [Fact]
        public void Deduct_ZeroUnits_ThrowsDomainException()
        {
            var stock = Build.Stock(quantity: 10);
            Assert.Throws<DomainException>(() => stock.Deduct(0));
        }

        [Fact]
        public void Add_ValidUnits_IncreasesQuantity()
        {
            var stock = Build.Stock(quantity: 10);
            stock.Add(5);
            Assert.Equal(15, stock.Quantity);
        }

        [Fact]
        public void Add_ZeroUnits_ThrowsDomainException()
        {
            var stock = Build.Stock(quantity: 10);
            Assert.Throws<DomainException>(() => stock.Add(0));
        }

        [Theory]
        [InlineData(5, 5, true)]   // igual al threshold → IsLow
        [InlineData(3, 5, true)]   // menor al threshold → IsLow
        [InlineData(6, 5, false)]  // mayor al threshold → no IsLow
        [InlineData(0, 5, false)]  // depleted → no IsLow (es IsDepleted)
        public void IsLow_ReturnsCorrectly(int quantity, int threshold, bool expected)
        {
            var stock = Build.Stock(quantity: quantity, threshold: threshold);
            Assert.Equal(expected, stock.IsLow);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void IsDepleted_ReturnsCorrectly(int quantity, bool expected)
        {
            var stock = Build.Stock(quantity: quantity);
            Assert.Equal(expected, stock.IsDepleted);
        }
    }
}
