using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(InventoryDbContext context, IPasswordHasher passwordHasher)
    {
        // Táblák létrehozása ha még nem léteznek
        await context.Database.EnsureCreatedAsync();

        const string targetEmail = "kundavid16@gmail.com";

        // 1. Felhasználók magvetése (Seed Users)
        if (!await context.Users.AnyAsync())
        {
            var defaultUsers = new List<AppUser>
            {
                new()
                {
                    Username = "raktaros",
                    FullName = "Kovács János (Raktáros)",
                    Email = targetEmail,
                    Role = "Raktaros",
                    PasswordHash = passwordHasher.HashPassword("Raktar123!"),
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Username = "beszerzo",
                    FullName = "Nagy Éva (Beszerző)",
                    Email = targetEmail,
                    Role = "Beszerzo",
                    PasswordHash = passwordHasher.HashPassword("Beszerzo123!"),
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Users.AddRange(defaultUsers);
            await context.SaveChangesAsync();
        }
        else
        {
            // Már meglévő felhasználók e-mail címének frissítése kundavid16@gmail.com-ra
            var existingUsers = await context.Users.ToListAsync();
            foreach (var u in existingUsers)
            {
                u.Email = targetEmail;
            }
            await context.SaveChangesAsync();
        }

        // 2. Termékek és mozgások inicializálása
        if (!await context.Products.AnyAsync())
        {
            var products = new List<Product>
            {
                new()
                {
                    Sku = "IT-LAP-001",
                    Name = "Dell Latitude 5540 Laptop 15.6\"",
                    Description = "Intel Core i7, 16GB RAM, 512GB SSD",
                    Category = "Számítástechnika",
                    Unit = "db",
                    UnitPrice = 385000m,
                    MinimumStockLevel = 4,
                    AutoReorderEnabled = true,
                    ReorderQuantity = 10,
                    SupplierName = "Dell Magyarország Disztribúció",
                    SupplierEmail = targetEmail,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new()
                {
                    Sku = "IT-MOU-002",
                    Name = "Logitech MX Master 3S Vezeték Nélküli Egér",
                    Description = "Csendes kattintás, 8000 DPI érzékelő",
                    Category = "Periféria",
                    Unit = "db",
                    UnitPrice = 38900m,
                    MinimumStockLevel = 5,
                    AutoReorderEnabled = true,
                    ReorderQuantity = 20,
                    SupplierName = "Logitech Partner Kft.",
                    SupplierEmail = targetEmail,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new()
                {
                    Sku = "IT-MON-003",
                    Name = "Dell UltraSharp 27\" 4K IPS Monitor",
                    Description = "U2723QE, USB-C Hub, 400 nits",
                    Category = "Kijelzők",
                    Unit = "db",
                    UnitPrice = 245000m,
                    MinimumStockLevel = 3,
                    AutoReorderEnabled = true,
                    ReorderQuantity = 5,
                    SupplierName = "Dell Magyarország Disztribúció",
                    SupplierEmail = targetEmail,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new()
                {
                    Sku = "IT-KEY-004",
                    Name = "Keychron K2 Pro Mechanikus Billentyűzet",
                    Description = "Vezeték nélküli, Hot-swappable, RGB",
                    Category = "Periféria",
                    Unit = "db",
                    UnitPrice = 42000m,
                    MinimumStockLevel = 4,
                    AutoReorderEnabled = true,
                    ReorderQuantity = 10,
                    SupplierName = "Periféria Nagyker Kft.",
                    SupplierEmail = targetEmail,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new()
                {
                    Sku = "NET-CAB-005",
                    Name = "Cat6 UTP Hálózati Kábel 3m",
                    Description = "Aranyozott RJ45 csatlakozók, szürke",
                    Category = "Hálózat",
                    Unit = "db",
                    UnitPrice = 1800m,
                    MinimumStockLevel = 15,
                    AutoReorderEnabled = true,
                    ReorderQuantity = 50,
                    SupplierName = "Kábel & Hálózat Kft.",
                    SupplierEmail = targetEmail,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Kezdeti beérkezések
            var initialMovements = new List<StockMovement>
            {
                new()
                {
                    ProductId = products[0].Id,
                    Type = MovementType.Inbound,
                    Quantity = 8,
                    MovementDate = DateTime.UtcNow.AddDays(-9),
                    ReferenceNumber = "SZALL-2026/001",
                    Notes = "Kezdőkészlet beérkezés - Hivatalos Dell disztribútor"
                },
                new()
                {
                    ProductId = products[1].Id,
                    Type = MovementType.Inbound,
                    Quantity = 25,
                    MovementDate = DateTime.UtcNow.AddDays(-9),
                    ReferenceNumber = "SZALL-2026/001",
                    Notes = "Kezdőkészlet beérkezés"
                },
                new()
                {
                    ProductId = products[2].Id,
                    Type = MovementType.Inbound,
                    Quantity = 5,
                    MovementDate = DateTime.UtcNow.AddDays(-7),
                    ReferenceNumber = "SZALL-2026/004",
                    Notes = "Dell monitor szállítmány"
                },
                new()
                {
                    ProductId = products[3].Id,
                    Type = MovementType.Inbound,
                    Quantity = 8,
                    MovementDate = DateTime.UtcNow.AddDays(-6),
                    ReferenceNumber = "SZALL-2026/005",
                    Notes = "Keychron import"
                },
                new()
                {
                    ProductId = products[4].Id,
                    Type = MovementType.Inbound,
                    Quantity = 10, // Kábel: 10 db érkezett, de a minimum 15! 10 < 15, így küszöb alatt van!
                    MovementDate = DateTime.UtcNow.AddDays(-4),
                    ReferenceNumber = "SZALL-2026/008",
                    Notes = "Kábelcsomag"
                },
                // Kiadás a laptopból (8 - 6 = 2 db marad, ami a 4-es minimum alá esik: 2 < 4!)
                new()
                {
                    ProductId = products[0].Id,
                    Type = MovementType.Outbound,
                    Quantity = 6,
                    MovementDate = DateTime.UtcNow.AddDays(-2),
                    ReferenceNumber = "KIAD-2026/012",
                    Notes = "Új fejlesztői munkaállomások felszerelése"
                }
            };

            context.StockMovements.AddRange(initialMovements);
            await context.SaveChangesAsync();
        }
    }
}
