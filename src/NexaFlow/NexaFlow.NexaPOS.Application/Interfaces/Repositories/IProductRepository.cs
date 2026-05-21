using NexaFlow.NexaPOS.Domain.Entities;

namespace NexaFlow.NexaPOS.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task SaveAsync(Product product);
        Task<Product?> GetByIdAsync(Guid tenantId, Guid productId);
        Task UpdateAsync(Product product);
        Task<bool> ExistsByNameAsync(Guid tenantId, string name);
        Task<(IEnumerable<(Product Product, int Stock, int LowStockThreshold)> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize);
    }
}
