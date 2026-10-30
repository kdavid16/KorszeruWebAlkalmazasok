using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryManager.Tests;

public class AuthServiceTests
{
    private InventoryDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsUserWithCorrectEmail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var hasher = new PasswordHasher();
        var user = new AppUser
        {
            Username = "raktaros",
            FullName = "Kovács János",
            Email = "kundavid16@gmail.com",
            Role = "Raktaros",
            PasswordHash = hasher.HashPassword("Raktar123!")
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, hasher);

        // Act
        var result = await authService.AuthenticateAsync("RAKTAROS", "Raktar123!");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("raktaros", result.Username);
        Assert.Equal("Raktaros", result.Role);
        Assert.Equal("kundavid16@gmail.com", result.Email);
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var hasher = new PasswordHasher();
        var user = new AppUser
        {
            Username = "beszerzo",
            FullName = "Nagy Éva",
            Email = "kundavid16@gmail.com",
            Role = "Beszerzo",
            PasswordHash = hasher.HashPassword("Beszerzo123!")
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, hasher);

        // Act
        var result = await authService.AuthenticateAsync("beszerzo", "RosszJelszo");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var hasher = new PasswordHasher();
        var authService = new AuthService(context, hasher);

        // Act
        var result = await authService.AuthenticateAsync("ismeretlen", "BarmilyenJelszo");

        // Assert
        Assert.Null(result);
    }
}
