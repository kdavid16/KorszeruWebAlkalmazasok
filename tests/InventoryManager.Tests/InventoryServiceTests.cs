using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryManager.Tests;

public class InventoryServiceTests
{
    private InventoryDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    private (InventoryService Service, ReorderService ReorderService, InventoryDbContext Context) CreateServices()
    {
        var context = CreateInMemoryDbContext();
        var emailSettings = Options.Create(new EmailSettings { RecipientEmail = "kundavid16@gmail.com", EnableRealSending = false });
        var emailService = new EmailNotificationService(context, emailSettings, NullLogger<EmailNotificationService>.Instance);
        var reorderService = new ReorderService(context, emailService, NullLogger<ReorderService>.Instance);
        var inventoryService = new InventoryService(context, reorderService, emailService, NullLogger<InventoryService>.Instance);
        return (inventoryService, reorderService, context);
    }

    [Fact]
    public async Task RecordReceiptAsync_ValidQuantity_CreatesInboundMovementAndIncreasesStock()
    {
        // Arrange
        var (service, _, context) = CreateServices();
        var product = new Product
        {
            Sku = "TEST-01",
            Name = "Teszt Termék",
            UnitPrice = 1000m,
            Unit = "db",
            MinimumStockLevel = 5
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var movement = await service.RecordReceiptAsync(product.Id, 15, "SZALL-001", "Kezdő raktározás");
        var currentStock = await service.GetProductStockAsync(product.Id);

        // Assert
        Assert.NotNull(movement);
        Assert.Equal(product.Id, movement.ProductId);
        Assert.Equal(15, movement.Quantity);
        Assert.Equal(MovementType.Inbound, movement.Type);
        Assert.Equal("SZALL-001", movement.ReferenceNumber);
        Assert.Equal(15, currentStock);
    }

    [Fact]
    public async Task RecordIssueAsync_ValidQuantityWithinStock_ReducesStock()
    {
        // Arrange
        var (service, _, context) = CreateServices();
        var product = new Product { Sku = "TEST-03", Name = "Termék 3", UnitPrice = 1200m };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        await service.RecordReceiptAsync(product.Id, 20, "IN-01", "Beérkezés");

        // Act
        var issueMovement = await service.RecordIssueAsync(product.Id, 7, "OUT-01", "Kiadás vevőnek");
        var remainingStock = await service.GetProductStockAsync(product.Id);

        // Assert
        Assert.NotNull(issueMovement);
        Assert.Equal(MovementType.Outbound, issueMovement.Type);
        Assert.Equal(7, issueMovement.Quantity);
        Assert.Equal(13, remainingStock); // 20 - 7 = 13
    }

    [Fact]
    public async Task RecordIssueAsync_StockWouldBecomeNegative_ThrowsInvalidOperationException()
    {
        // SZABÁLY: Kiadás után a készlet NEM lehet negatív!
        // Arrange
        var (service, _, context) = CreateServices();
        var product = new Product { Sku = "TEST-04", Name = "Termék 4", UnitPrice = 800m };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        await service.RecordReceiptAsync(product.Id, 5, "IN-01", "Kevés beérkezés");

        // Act & Assert (5 - 6 = -1 -> Negatív lenne!)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordIssueAsync(product.Id, 6, "OUT-01", "Túl sok kiadás"));

        Assert.Contains("negatív", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task GetStockListAsync_CalculatesCorrectStockForEachProduct()
    {
        // Arrange
        var (service, _, context) = CreateServices();
        var p1 = new Product { Sku = "P-1", Name = "Egér", UnitPrice = 3000m };
        var p2 = new Product { Sku = "P-2", Name = "Billentyűzet", UnitPrice = 7000m };
        context.Products.AddRange(p1, p2);
        await context.SaveChangesAsync();

        await service.RecordReceiptAsync(p1.Id, 10, "IN-1", null);
        await service.RecordIssueAsync(p1.Id, 3, "OUT-1", null);

        await service.RecordReceiptAsync(p2.Id, 5, "IN-2", null);

        // Act
        var stockList = await service.GetStockListAsync();

        // Assert
        var item1 = stockList.Single(i => i.ProductId == p1.Id);
        var item2 = stockList.Single(i => i.ProductId == p2.Id);

        Assert.Equal(7, item1.CurrentStock); // 10 - 3 = 7
        Assert.Equal(21000m, item1.TotalValue); // 7 * 3000

        Assert.Equal(5, item2.CurrentStock);
        Assert.Equal(35000m, item2.TotalValue); // 5 * 7000
    }
}
