using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Web.Services;

public class ProductService : IProductService
{
    private readonly InventoryDbContext _context;

    public ProductService(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.StockMovements.OrderByDescending(sm => sm.MovementDate))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        product.Sku = product.Sku.Trim().ToUpperInvariant();
        product.Name = product.Name.Trim();

        if (await ExistsSkuAsync(product.Sku))
        {
            throw new InvalidOperationException($"Már létezik termék ezzel a cikkszámmal: '{product.Sku}'.");
        }

        if (product.UnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(product.UnitPrice), "Az egységár nem lehet negatív.");
        }

        product.CreatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<bool> ExistsSkuAsync(string sku, int? excludeProductId = null)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        return await _context.Products
            .AnyAsync(p => p.Sku == normalizedSku && (!excludeProductId.HasValue || p.Id != excludeProductId.Value));
    }
}
