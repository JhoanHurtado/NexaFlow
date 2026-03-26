using Moq;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Services;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Events;
using NexaFlow.NexaPOS.Domain.Exceptions;
using NexaFlow.NexaPOS.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaPOS.Tests.Application
{
    public class SaleServiceTests
    {
        private readonly Mock<ISaleRepository> _saleRepoMock = new();
        private readonly Mock<IProductRepository> _productRepoMock = new();
        private readonly Mock<IStockRepository> _stockRepoMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPosLogger> _loggerMock = new();

        private SaleService CreateService() => new(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _stockRepoMock.Object,
            _uowMock.Object,
            _loggerMock.Object);

        private void SetupProduct(Product product) =>
            _productRepoMock.Setup(r => r.GetByIdAsync(Build.TenantId, product.Id))
                            .ReturnsAsync(product);

        private void SetupStock(ProductStock stock) =>
            _stockRepoMock.Setup(r => r.GetByProductIdAsync(Build.TenantId, stock.ProductId))
                          .ReturnsAsync(stock);

        [Fact]
        public async Task CreateAsync_ValidSale_ReturnsId_AndEnqueuesEvents()
        {
            var product = Build.Product(price: 10m);
            var stock = Build.Stock(productId: product.Id, quantity: 50);
            SetupProduct(product);
            SetupStock(stock);

            var request = new CreateSaleRequest(null, null,
                new[] { new CreateSaleItemRequest(product.Id, 2) });

            var id = await CreateService().CreateAsync(Build.TenantId, request);

            Assert.NotEqual(Guid.Empty, id);
            _uowMock.Verify(u => u.SaveSaleAsync(It.IsAny<Sale>()), Times.Once);
            _uowMock.Verify(u => u.SaveSaleItemsAsync(It.IsAny<IEnumerable<SaleItem>>()), Times.Once);
            _uowMock.Verify(u => u.UpdateStockAsync(It.IsAny<ProductStock>()), Times.Once);
            _uowMock.Verify(u => u.EnqueueEventAsync(It.IsAny<SaleCreatedEvent>()), Times.Once);
            _uowMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_EmptyItems_ThrowsDomainException()
        {
            var request = new CreateSaleRequest(null, null, Array.Empty<CreateSaleItemRequest>());

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().CreateAsync(Build.TenantId, request));

            _uowMock.Verify(u => u.BeginAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ZeroQuantity_ThrowsDomainException()
        {
            var product = Build.Product();
            var request = new CreateSaleRequest(null, null,
                new[] { new CreateSaleItemRequest(product.Id, 0) });

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().CreateAsync(Build.TenantId, request));
        }

        [Fact]
        public async Task CreateAsync_DuplicateProduct_ThrowsDomainException()
        {
            var productId = Guid.NewGuid();
            var request = new CreateSaleRequest(null, null, new[]
            {
                new CreateSaleItemRequest(productId, 1),
                new CreateSaleItemRequest(productId, 2)
            });

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().CreateAsync(Build.TenantId, request));
        }

        [Fact]
        public async Task CreateAsync_ProductNotFound_ThrowsDomainException()
        {
            var productId = Guid.NewGuid();
            _productRepoMock.Setup(r => r.GetByIdAsync(Build.TenantId, productId))
                            .ReturnsAsync((Product?)null);

            var request = new CreateSaleRequest(null, null,
                new[] { new CreateSaleItemRequest(productId, 1) });

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().CreateAsync(Build.TenantId, request));
        }

        [Fact]
        public async Task CreateAsync_InsufficientStock_ThrowsDomainException()
        {
            var product = Build.Product(price: 10m);
            var stock = Build.Stock(productId: product.Id, quantity: 2);
            SetupProduct(product);
            SetupStock(stock);

            var request = new CreateSaleRequest(null, null,
                new[] { new CreateSaleItemRequest(product.Id, 5) });

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().CreateAsync(Build.TenantId, request));

            _uowMock.Verify(u => u.BeginAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_StockBecomesLow_EnqueuesStockLowEvent()
        {
            var product = Build.Product(price: 10m);
            // quantity=6, threshold=5 → después de deducir 2 queda 4 → IsLow
            var stock = Build.Stock(productId: product.Id, quantity: 6, threshold: 5);
            SetupProduct(product);
            SetupStock(stock);

            var request = new CreateSaleRequest(null, null,
                new[] { new CreateSaleItemRequest(product.Id, 2) });

            await CreateService().CreateAsync(Build.TenantId, request);

            _uowMock.Verify(u => u.EnqueueEventAsync(It.IsAny<StockLowEvent>()), Times.Once);
            _uowMock.Verify(u => u.EnqueueEventAsync(It.IsAny<SaleCreatedEvent>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_StockBecomesDepleted_EnqueuesStockDepletedEvent()
        {
            var product = Build.Product(price: 10m);
            var stock = Build.Stock(productId: product.Id, quantity: 3, threshold: 5);
            SetupProduct(product);
            SetupStock(stock);

            var request = new CreateSaleRequest(null, null,
                new[] { new CreateSaleItemRequest(product.Id, 3) });

            await CreateService().CreateAsync(Build.TenantId, request);

            _uowMock.Verify(u => u.EnqueueEventAsync(It.IsAny<StockDepletedEvent>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenUowThrows_RollsBack()
        {
            var product = Build.Product(price: 10m);
            var stock = Build.Stock(productId: product.Id, quantity: 50);
            SetupProduct(product);
            SetupStock(stock);

            _uowMock.Setup(u => u.SaveSaleAsync(It.IsAny<Sale>()))
                    .ThrowsAsync(new Exception("DB error"));

            var request = new CreateSaleRequest(null, null,
                new[] { new CreateSaleItemRequest(product.Id, 1) });

            await Assert.ThrowsAsync<Exception>(() =>
                CreateService().CreateAsync(Build.TenantId, request));

            _uowMock.Verify(u => u.RollbackAsync(), Times.Once);
            _uowMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithCustomerAndReservation_PassesThrough()
        {
            var product = Build.Product(price: 5m);
            var stock = Build.Stock(productId: product.Id, quantity: 10);
            var customerId = Guid.NewGuid();
            var reservationId = Guid.NewGuid();
            SetupProduct(product);
            SetupStock(stock);

            var request = new CreateSaleRequest(customerId, reservationId,
                new[] { new CreateSaleItemRequest(product.Id, 1) });

            var id = await CreateService().CreateAsync(Build.TenantId, request);

            Assert.NotEqual(Guid.Empty, id);
            _uowMock.Verify(u => u.SaveSaleAsync(It.Is<Sale>(s =>
                s.CustomerId == customerId && s.ReservationId == reservationId)), Times.Once);
        }

        [Fact]
        public async Task GetSaleByIdAsync_EmptyGuid_ThrowsDomainException()
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().GetSaleByIdAsync(Build.TenantId, Guid.Empty));
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task ListSalesAsync_InvalidPagination_ThrowsDomainException(int page, int pageSize)
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().ListSalesAsync(Build.TenantId, page, pageSize));
        }
    }
}
