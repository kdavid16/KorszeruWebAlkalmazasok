using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using InventoryManager.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Web.Services;

public class InventoryService : IInventoryService
{
    private readonly InventoryDbContext _context;
    private readonly IReorderService _reorderService;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        InventoryDbContext context, 
        IReorderService reorderService,
        IEmailNotificationService emailService,
        ILogger<InventoryService> logger)
    {
        _context = context;
        _reorderService = reorderService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<StockDashboardViewModel> GetDashboardSummaryAsync()
    {
        var items = await GetStockListAsync();

        return new StockDashboardViewModel
        {
            TotalProducts = items.Count,
            TotalItemsInStock = items.Sum(i => i.CurrentStock),
            TotalInventoryValue = items.Sum(i => i.TotalValue),
            LowStockCount = items.Count(i => i.IsLowStock),
            OutOfStockCount = items.Count(i => i.IsOutOfStock),
            Items = items
        };
    }

    public async Task<IReadOnlyList<ProductStockSummaryViewModel>> GetStockListAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.StockMovements)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var summaries = products.Select(p =>
        {
            int inbound = p.StockMovements.Where(m => m.Type == MovementType.Inbound).Sum(m => m.Quantity);
            int outbound = p.StockMovements.Where(m => m.Type == MovementType.Outbound).Sum(m => m.Quantity);
            int adjustment = p.StockMovements.Where(m => m.Type == MovementType.Adjustment).Sum(m => m.Quantity);
            int currentStock = inbound - outbound + adjustment;

            return new ProductStockSummaryViewModel
            {
                ProductId = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Category = p.Category,
                Unit = p.Unit,
                UnitPrice = p.UnitPrice,
                MinimumStockLevel = p.MinimumStockLevel,
                CurrentStock = currentStock
            };
        }).ToList();

        return summaries;
    }

    public async Task<int> GetProductStockAsync(int productId)
    {
        var movements = await _context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.ProductId == productId)
            .ToListAsync();

        int inbound = movements.Where(m => m.Type == MovementType.Inbound).Sum(m => m.Quantity);
        int outbound = movements.Where(m => m.Type == MovementType.Outbound).Sum(m => m.Quantity);
        int adjustment = movements.Where(m => m.Type == MovementType.Adjustment).Sum(m => m.Quantity);

        return inbound - outbound + adjustment;
    }

    public async Task<StockMovement> RecordReceiptAsync(int productId, int quantity, string? referenceNumber, string? notes)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A beérkezési mennyiségnek 0-nál nagyobbnak kell lennie.");
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Nem található termék a következő azonosítóval: {productId}");
        }

        var movement = new StockMovement
        {
            ProductId = productId,
            Type = MovementType.Inbound,
            Quantity = quantity,
            MovementDate = DateTime.UtcNow,
            ReferenceNumber = referenceNumber?.Trim(),
            Notes = notes?.Trim()
        };

        _context.StockMovements.Add(movement);

        // Ha volt megrendelt (Ordered) utánrendelés erre a termékre, teljesítetté jelöljük
        var pendingOrOrdered = await _context.ReorderOrders
            .Where(ro => ro.ProductId == productId && ro.Status == ReorderStatus.Ordered)
            .OrderBy(ro => ro.CreatedAt)
            .FirstOrDefaultAsync();

        if (pendingOrOrdered != null)
        {
            pendingOrOrdered.Status = ReorderStatus.Received;
            pendingOrOrdered.FulfilledAt = DateTime.UtcNow;
            pendingOrOrdered.Notes = (pendingOrOrdered.Notes != null ? pendingOrOrdered.Notes + " | " : "") + 
                                     $"Átvéve a raktárba ({movement.ReferenceNumber ?? "bizonylat nélkül"}).";
        }

        await _context.SaveChangesAsync();

        // Email értesítő az árubevételezésről
        try
        {
            await _emailService.SendStockReceivedNotificationAsync(product, quantity, referenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nem sikerült e-mail értesítőt küldeni az árubevételezésről");
        }

        return movement;
    }

    public async Task<StockMovement> RecordIssueAsync(int productId, int quantity, string? referenceNumber, string? notes)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A kiadási mennyiségnek 0-nál nagyobbnak kell lennie.");
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Nem található termék a következő azonosítóval: {productId}");
        }

        var currentStock = await GetProductStockAsync(productId);

        // SZABÁLY: Kiadás után a készlet nem lehet negatív!
        if (currentStock - quantity < 0)
        {
            throw new InvalidOperationException($"A kiadás nem hajtható végre, mert a raktárkészlet negatívvá válna! Jelenlegi készlet: {currentStock} {product.Unit}, kiadandó: {quantity} {product.Unit}.");
        }

        var movement = new StockMovement
        {
            ProductId = productId,
            Type = MovementType.Outbound,
            Quantity = quantity,
            MovementDate = DateTime.UtcNow,
            ReferenceNumber = referenceNumber?.Trim(),
            Notes = notes?.Trim()
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        // Kiadás után azonnal lefuttatjuk az automatikus utánrendelési szabályellenőrzést!
        await _reorderService.CheckAndGenerateAutoReordersAsync();

        return movement;
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int? productId = null)
    {
        var query = _context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(sm => sm.ProductId == productId.Value);
        }

        return await query
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync();
    }
}
