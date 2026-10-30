using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryManager.Tests;

public class EmailNotificationServiceTests
{
    private InventoryDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    [Fact]
    public async Task SendNotificationAsync_SimulatedMode_LogsNotificationToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var settings = Options.Create(new EmailSettings
        {
            RecipientEmail = "kundavid16@gmail.com",
            EnableRealSending = false
        });
        var service = new EmailNotificationService(context, settings, NullLogger<EmailNotificationService>.Instance);

        // Act
        var result = await service.SendNotificationAsync("kundavid16@gmail.com", "Teszt Értesítés", "<p>Tartalom</p>");

        // Assert
        Assert.True(result.Success);
        Assert.False(result.WasRealSmtpAttempted);

        var logs = await service.GetRecentLogsAsync();
        Assert.Single(logs);
        Assert.Equal("kundavid16@gmail.com", logs[0].Recipient);
        Assert.Equal("Teszt Értesítés", logs[0].Subject);
        Assert.True(logs[0].IsSuccess);
    }

    [Fact]
    public async Task SendNotificationAsync_RealSendingEnabledWithoutCredentials_ReturnsFailureAndLogsError()
    {
        // Arrange: EnableRealSending = true, de a Username és Password üres
        using var context = CreateInMemoryDbContext();
        var settings = Options.Create(new EmailSettings
        {
            RecipientEmail = "kundavid16@gmail.com",
            EnableRealSending = true,
            Username = "",
            Password = ""
        });
        var service = new EmailNotificationService(context, settings, NullLogger<EmailNotificationService>.Instance);

        // Act
        var result = await service.SendNotificationAsync("kundavid16@gmail.com", "Hiba Teszt", "<p>Tartalom</p>");

        // Assert: Világos hibaüzenetet kell adnia, nem szabad csendben sikeresnek lennie!
        Assert.False(result.Success);
        Assert.True(result.WasRealSmtpAttempted);
        Assert.Contains("Username", result.StatusMessage);

        var logs = await service.GetRecentLogsAsync();
        Assert.Single(logs);
        Assert.False(logs[0].IsSuccess);
        Assert.Contains("Username", logs[0].StatusMessage);
    }

    [Fact]
    public async Task SendStockReceivedNotificationAsync_CreatesLogWithQuantityAndProduct()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var settings = Options.Create(new EmailSettings
        {
            RecipientEmail = "kundavid16@gmail.com",
            EnableRealSending = false
        });
        var service = new EmailNotificationService(context, settings, NullLogger<EmailNotificationService>.Instance);
        var product = new Product { Sku = "SKU-NOTIF", Name = "Teszt Termék", Unit = "db" };

        // Act
        var result = await service.SendStockReceivedNotificationAsync(product, 25, "SZALL-999");

        // Assert
        Assert.True(result.Success);
        var logs = await service.GetRecentLogsAsync();
        Assert.Single(logs);
        Assert.Equal("kundavid16@gmail.com", logs[0].Recipient);
        Assert.Contains("Új Árubevételezés", logs[0].Subject);
        Assert.Contains("+25 db", logs[0].Body);
    }
}
