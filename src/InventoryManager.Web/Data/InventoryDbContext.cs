using InventoryManager.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Web.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ReorderOrder> ReorderOrders => Set<ReorderOrder>();
    public DbSet<EmailNotificationLog> EmailLogs => Set<EmailNotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Termék beállítások
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");

            entity.HasMany(p => p.StockMovements)
                  .WithOne(sm => sm.Product)
                  .HasForeignKey(sm => sm.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.ReorderOrders)
                  .WithOne(ro => ro.Product)
                  .HasForeignKey(ro => ro.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Mozgás beállítások
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(sm => sm.Id);
            entity.Property(sm => sm.Quantity).IsRequired();
            entity.Property(sm => sm.Type).IsRequired();
            entity.Property(sm => sm.ReferenceNumber).HasMaxLength(100);
            entity.Property(sm => sm.Notes).HasMaxLength(500);
        });

        // Felhasználók beállításai
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(30);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
        });

        // Utánrendelések beállításai
        modelBuilder.Entity<ReorderOrder>(entity =>
        {
            entity.HasKey(ro => ro.Id);
            entity.HasIndex(ro => ro.OrderNumber).IsUnique();
            entity.Property(ro => ro.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(ro => ro.EstimatedTotalCost).HasColumnType("decimal(18,2)");
        });

        // Email értesítés napló
        modelBuilder.Entity<EmailNotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipient).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).IsRequired();
        });
    }
}
