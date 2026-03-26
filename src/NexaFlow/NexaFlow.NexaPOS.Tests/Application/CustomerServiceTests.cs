using Moq;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Services;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Events;
using NexaFlow.NexaPOS.Domain.Exceptions;
using NexaFlow.NexaPOS.Tests.Helpers;
using Xunit;

namespace NexaFlow.NexaPOS.Tests.Application
{
    public class CustomerServiceTests
    {
        private readonly Mock<ICustomerRepository> _repoMock = new();
        private readonly Mock<IEventRepository> _eventsMock = new();
        private readonly Mock<IPosLogger> _loggerMock = new();

        private CustomerService CreateService() =>
            new(_repoMock.Object, _eventsMock.Object, _loggerMock.Object);

        [Fact]
        public async Task CreateAsync_ValidRequest_ReturnsId_AndPublishesEvent()
        {
            var request = new CreateCustomerRequest("Juan Pérez", "3001234567", "juan@test.com");

            var id = await CreateService().CreateAsync(Build.TenantId, request);

            Assert.NotEqual(Guid.Empty, id);
            _repoMock.Verify(r => r.SaveAsync(It.Is<Customer>(c =>
                c.Name == "Juan Pérez" && c.TenantId == Build.TenantId)), Times.Once);
            _eventsMock.Verify(e => e.PublishAsync(It.IsAny<CustomerCreatedEvent>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_EmptyName_ThrowsDomainException(string name)
        {
            var request = new CreateCustomerRequest(name, null, null);

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().CreateAsync(Build.TenantId, request));

            _repoMock.Verify(r => r.SaveAsync(It.IsAny<Customer>()), Times.Never);
            _eventsMock.Verify(e => e.PublishAsync(It.IsAny<CustomerCreatedEvent>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_InvalidEmail_ThrowsDomainException()
        {
            var request = new CreateCustomerRequest("Juan", null, "not-an-email");

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().CreateAsync(Build.TenantId, request));
        }

        [Fact]
        public async Task CreateAsync_NullEmailAndPhone_IsAllowed()
        {
            var request = new CreateCustomerRequest("Juan", null, null);

            var id = await CreateService().CreateAsync(Build.TenantId, request);

            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public async Task ListCustomersAsync_ValidParams_ReturnsMappedDtos()
        {
            var customers = new List<Customer>
            {
                Build.Customer("Ana"),
                Build.Customer("Luis")
            };
            _repoMock.Setup(r => r.GetPagedAsync(Build.TenantId, 1, 10))
                     .ReturnsAsync((customers, 2));

            var result = await CreateService().ListCustomersAsync(Build.TenantId, 1, 10);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count());
            Assert.Equal(2, result.Pagination!.TotalCount);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task ListCustomersAsync_InvalidPagination_ThrowsDomainException(int page, int pageSize)
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateService().ListCustomersAsync(Build.TenantId, page, pageSize));
        }
    }
}
