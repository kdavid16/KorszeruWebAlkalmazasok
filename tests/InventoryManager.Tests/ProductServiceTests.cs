using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryManager.Tests;

public class ProductServiceTests
{
    private InventoryDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ValidProduct_SavesAndNormalizesSku()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new ProductService(context);

        var product = new Product
        {
            Sku = "  sku-100  ",
            Name = "  Teszt Termék 1  ",
            UnitPrice = 2500m,
            Unit = "db"
        };

        // Act
        var created = await service.CreateAsync(product);

        // Assert
        Assert.True(created.Id > 0);
        Assert.Equal("SKU-100", created.Sku); // Uppercase + trimmed
        Assert.Equal("Teszt Termék 1", created.Name); // Trimmed
        Assert.Single(context.Products);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSku_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new ProductService(context);

        var existing = new Product { Sku = "SKU-DUPLICATE", Name = "Első termék", UnitPrice = 1000m };
        await service.CreateAsync(existing);

        var duplicate = new Product { Sku = "sku-duplicate", Name = "Második termék", UnitPrice = 2000m };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(duplicate));
        Assert.Contains("Már létezik termék", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_NegativePrice_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new ProductService(context);

        var product = new Product
        {
            Sku = "SKU-NEG",
            Name = "Hibás árú termék",
            UnitPrice = -50m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateAsync(product));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProductsOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new ProductService(context);

        context.Products.AddRange(
            new Product { Sku = "Z", Name = "Zsiráf plüss", UnitPrice = 100m },
            new Product { Sku = "A", Name = "Alma", UnitPrice = 200m },
            new Product { Sku = "B", Name = "Banán", UnitPrice = 300m }
        );
        await context.SaveChangesAsync();

        // Act
        var list = await service.GetAllAsync();

        // Assert
        Assert.Equal(3, list.Count);
        Assert.Equal("Alma", list[0].Name);
        Assert.Equal("Banán", list[1].Name);
        Assert.Equal("Zsiráf plüss", list[2].Name);
    }
}
