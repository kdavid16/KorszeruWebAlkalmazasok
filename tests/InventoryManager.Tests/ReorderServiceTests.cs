using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryManager.Tests;

public class ReorderServiceTests
{
    private InventoryDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    private (ReorderService Service, EmailNotificationService EmailService, InventoryDbContext Context) CreateServices()
    {
        var context = CreateInMemoryDbContext();
        var emailSettings = Options.Create(new EmailSettings
        {
            RecipientEmail = "kundavid16@gmail.com",
            EnableRealSending = false
        });
        var emailService = new EmailNotificationService(context, emailSettings, NullLogger<EmailNotificationService>.Instance);
        var reorderService = new ReorderService(context, emailService, NullLogger<ReorderService>.Instance);
        return (reorderService, emailService, context);
    }

    [Fact]
    public async Task CheckAndGenerateAutoReordersAsync_StockStrictlyBelowMinimum_GeneratesReorderOrderAndSendsEmail()
    {
        // SZABÁLY: Csak a küszöb alatt (<) keletkezik utánrendelés!
        // 4 < 5 -> Keletkezik!
        var (service, _, context) = CreateServices();
        var product = new Product
        {
            Sku = "BELOW-01",
            Name = "Monitor Kábel",
            UnitPrice = 2000m,
            MinimumStockLevel = 5,
            AutoReorderEnabled = true,
            ReorderQuantity = 30
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // 4 db készlet (4 < 5: szigorúan a küszöb alatt)
        context.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Type = MovementType.Inbound,
            Quantity = 4
        });
        await context.SaveChangesAsync();

        // Act
        int generatedCount = await service.CheckAndGenerateAutoReordersAsync();
        var orders = await service.GetAllOrdersAsync();
        var emailLogs = await context.EmailLogs.ToListAsync();

        // Assert
        Assert.Equal(1, generatedCount);
        Assert.Single(orders);
        Assert.Equal(30, orders[0].Quantity);
        Assert.Equal(ReorderStatus.Pending, orders[0].Status);

        // Email értesítő ellenőrzése
        Assert.Single(emailLogs);
        Assert.Equal("kundavid16@gmail.com", emailLogs[0].Recipient);
        Assert.Contains("Utánrendelés", emailLogs[0].Subject);
    }

    [Fact]
    public async Task CheckAndGenerateAutoReordersAsync_StockEqualToMinimum_DoesNotGenerateOrder()
    {
        // SZABÁLY: Készlet == Küszöb esetén MÉG NEM keletkezik utánrendelés, csak ALATTA (<)!
        var (service, _, context) = CreateServices();
        var product = new Product
        {
            Sku = "EQUAL-01",
            Name = "Egérpad",
            UnitPrice = 3000m,
            MinimumStockLevel = 5,
            AutoReorderEnabled = true,
            ReorderQuantity = 20
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Pontosan 5 db készlet (5 == 5 -> nem küszöb ALATTI!)
        context.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Type = MovementType.Inbound,
            Quantity = 5
        });
        await context.SaveChangesAsync();

        // Act
        int generatedCount = await service.CheckAndGenerateAutoReordersAsync();
        var orders = await service.GetAllOrdersAsync();

        // Assert
        Assert.Equal(0, generatedCount);
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CheckAndGenerateAutoReordersAsync_StockAboveMinimum_DoesNotGenerateOrder()
    {
        var (service, _, context) = CreateServices();
        var product = new Product
        {
            Sku = "OK-01",
            Name = "Laptop",
            UnitPrice = 300000m,
            MinimumStockLevel = 5,
            AutoReorderEnabled = true,
            ReorderQuantity = 10
        };
        context.Products.Add(product);
        context.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Type = MovementType.Inbound,
            Quantity = 15 // 15 > 5
        });
        await context.SaveChangesAsync();

        int generatedCount = await service.CheckAndGenerateAutoReordersAsync();

        Assert.Equal(0, generatedCount);
        Assert.Empty(await service.GetAllOrdersAsync());
    }

    [Fact]
    public async Task CheckAndGenerateAutoReordersAsync_AutoReorderDisabled_DoesNotGenerateOrder()
    {
        var (service, _, context) = CreateServices();
        var product = new Product
        {
            Sku = "MANUAL-01",
            Name = "Egyedi gép",
            MinimumStockLevel = 5,
            AutoReorderEnabled = false // Letiltva!
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        int generatedCount = await service.CheckAndGenerateAutoReordersAsync();

        Assert.Equal(0, generatedCount);
        Assert.Empty(await service.GetAllOrdersAsync());
    }

    [Fact]
    public async Task CheckAndGenerateAutoReordersAsync_ExistingActiveOrder_DoesNotDuplicate()
    {
        var (service, _, context) = CreateServices();
        var product = new Product
        {
            Sku = "DUP-01",
            Name = "Egér",
            MinimumStockLevel = 5,
            AutoReorderEnabled = true,
            ReorderQuantity = 15
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        int firstRun = await service.CheckAndGenerateAutoReordersAsync();
        Assert.Equal(1, firstRun);

        int secondRun = await service.CheckAndGenerateAutoReordersAsync();
        Assert.Equal(0, secondRun);

        var orders = await service.GetAllOrdersAsync();
        Assert.Single(orders);
    }

    [Fact]
    public async Task ApproveOrderAsync_PendingOrder_SetsStatusToOrderedAndSendsEmail()
    {
        var (service, _, context) = CreateServices();
        var product = new Product { Sku = "P-APP", Name = "Teszt", MinimumStockLevel = 5, AutoReorderEnabled = true };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        await service.CheckAndGenerateAutoReordersAsync();
        var order = (await service.GetAllOrdersAsync()).First();

        var approved = await service.ApproveOrderAsync(order.Id);

        Assert.Equal(ReorderStatus.Ordered, approved.Status);
        Assert.NotNull(approved.OrderedAt);

        var emailLogs = await context.EmailLogs.ToListAsync();
        Assert.Contains(emailLogs, e => e.Subject.Contains("Jóváhagyva"));
    }

    [Fact]
    public async Task IssueMovement_AutomaticallyTriggersReorder_WhenStockDropsStrictlyBelowThreshold()
    {
        var (reorderService, emailService, context) = CreateServices();
        var inventoryService = new InventoryService(context, reorderService, emailService, NullLogger<InventoryService>.Instance);

        var product = new Product
        {
            Sku = "AUTO-TRIG-01",
            Name = "Kamera",
            UnitPrice = 10000m,
            MinimumStockLevel = 3,
            AutoReorderEnabled = true,
            ReorderQuantity = 10
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // 5 db beérkezés (5 > 3 -> nincs utánrendelés)
        await inventoryService.RecordReceiptAsync(product.Id, 5, "IN-01", "Kezdőkészlet");
        Assert.Empty(await reorderService.GetAllOrdersAsync());

        // Kiadunk 2 db-ot: 5 - 2 = 3 db (3 == 3 -> küszöbön van, még NEM ALATTA!)
        await inventoryService.RecordIssueAsync(product.Id, 2, "OUT-01", "Kiadás");
        Assert.Empty(await reorderService.GetAllOrdersAsync());

        // Kiadunk még 1 db-ot: 3 - 1 = 2 db (2 < 3 -> szigorúan a KÜSZÖB ALATT!)
        await inventoryService.RecordIssueAsync(product.Id, 1, "OUT-02", "Kiadás");

        var orders = await reorderService.GetAllOrdersAsync();
        Assert.Single(orders);
        Assert.Equal(ReorderStatus.Pending, orders[0].Status);
        Assert.Equal(10, orders[0].Quantity);
    }
}
