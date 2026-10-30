using InventoryManager.Web.Models;

namespace InventoryManager.Web.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<bool> ExistsSkuAsync(string sku, int? excludeProductId = null);
}
