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
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _repoMock = new();
        private readonly Mock<IStockRepository> _stockRepoMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPosLogger> _loggerMock = new();

        private ProductService CreateService() =>
            new(_repoMock.Object, _stockRepoMock.Object, _uowMock.Object, _loggerMock.Object);

        [Fact]
        public async Task CreateAsync_ValidRequest_ReturnsNewGuid()
        {
            var request = new CreateProductRequest("Coca Cola", 1.50m, 100, 10);
            var service = CreateService();

            var id = await service.CreateAsync(Build.TenantId, request);

            Assert.NotEqual(Guid.Empty, id);
            _uowMock.Verify(u => u.BeginAsync(Build.TenantId), Times.Once);
            _uowMock.Verify(u => u.SaveProductAsync(It.IsAny<Product>()), Times.Once);
            _uowMock.Verify(u => u.SaveStockAsync(It.IsAny<ProductStock>()), Times.Once);
            _uowMock.Verify(u => u.EnqueueEventAsync(It.IsAny<ProductCreatedEvent>()), Times.Once);
            _uowMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ZeroPrice_ThrowsDomainException()
        {
            var request = new CreateProductRequest("Test", 0m);
            var service = CreateService();

            await Assert.ThrowsAsync<DomainException>(() =>
                service.CreateAsync(Build.TenantId, request));

            _uowMock.Verify(u => u.BeginAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NegativePrice_ThrowsDomainException()
        {
            var request = new CreateProductRequest("Test", -5m);
            var service = CreateService();

            await Assert.ThrowsAsync<DomainException>(() =>
                service.CreateAsync(Build.TenantId, request));
        }

        [Fact]
        public async Task CreateAsync_NegativeStock_ThrowsDomainException()
        {
            var request = new CreateProductRequest("Test", 10m, InitialStock: -1);
            var service = CreateService();

            await Assert.ThrowsAsync<DomainException>(() =>
                service.CreateAsync(Build.TenantId, request));
        }

        [Fact]
        public async Task CreateAsync_WhenUowThrows_RollsBack()
        {
            _uowMock.Setup(u => u.SaveProductAsync(It.IsAny<Product>()))
                    .ThrowsAsync(new Exception("DB error"));

            var request = new CreateProductRequest("Test", 10m, 50, 5);
            var service = CreateService();

            await Assert.ThrowsAsync<Exception>(() =>
                service.CreateAsync(Build.TenantId, request));

            _uowMock.Verify(u => u.RollbackAsync(), Times.Once);
            _uowMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task GetPagedAsync_ValidParams_ReturnsPagedResponse()
        {
            var products = new List<(Product Product, int Stock, int LowStockThreshold)>
            {
                (Build.Product("P1", 5m), 10, 5),
                (Build.Product("P2", 10m), 20, 5)
            };
            
            _repoMock.Setup(r => r.GetPagedAsync(Build.TenantId, 1, 10))
                     .ReturnsAsync((products.AsEnumerable(), 2));

            var service = CreateService();
            var result = await service.GetPagedAsync(Build.TenantId, 1, 10);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count());
            Assert.Equal(1, result.Pagination!.CurrentPage);
            Assert.Equal(2, result.Pagination.TotalCount);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task GetPagedAsync_InvalidPagination_ThrowsDomainException(int page, int pageSize)
        {
            var service = CreateService();
            await Assert.ThrowsAsync<DomainException>(() =>
                service.GetPagedAsync(Build.TenantId, page, pageSize));
        }
    }
}
