using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Web.Services;

public class ReorderService : IReorderService
{
    private readonly InventoryDbContext _context;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<ReorderService> _logger;

    public ReorderService(
        InventoryDbContext context, 
        IEmailNotificationService emailService,
        ILogger<ReorderService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReorderOrder>> GetAllOrdersAsync(ReorderStatus? status = null)
    {
        var query = _context.ReorderOrders
            .AsNoTracking()
            .Include(ro => ro.Product)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(ro => ro.Status == status.Value);
        }

        return await query
            .OrderByDescending(ro => ro.CreatedAt)
            .ToListAsync();
    }

    public async Task<ReorderOrder?> GetOrderByIdAsync(int id)
    {
        return await _context.ReorderOrders
            .Include(ro => ro.Product)
            .FirstOrDefaultAsync(ro => ro.Id == id);
    }

    public async Task<int> CheckAndGenerateAutoReordersAsync()
    {
        // 1. Azok a termékek, ahol az automata utánrendelés aktív
        var products = await _context.Products
            .Include(p => p.StockMovements)
            .Where(p => p.AutoReorderEnabled)
            .ToListAsync();

        int generatedCount = 0;

        foreach (var product in products)
        {
            // Készletszámítás
            int inbound = product.StockMovements.Where(m => m.Type == MovementType.Inbound).Sum(m => m.Quantity);
            int outbound = product.StockMovements.Where(m => m.Type == MovementType.Outbound).Sum(m => m.Quantity);
            int adjustment = product.StockMovements.Where(m => m.Type == MovementType.Adjustment).Sum(m => m.Quantity);
            int currentStock = inbound - outbound + adjustment;

            // SZABÁLY: Utánrendelési jelzés CSAK a megadott küszöb ALATT (<) keletkezik!
            if (currentStock < product.MinimumStockLevel)
            {
                // Ellenőrizzük, van-e már nyitott (Pending vagy Ordered) utánrendelés erre a termékre
                bool hasActiveOrder = await _context.ReorderOrders
                    .AnyAsync(ro => ro.ProductId == product.Id && 
                                   (ro.Status == ReorderStatus.Pending || ro.Status == ReorderStatus.Ordered));

                if (!hasActiveOrder)
                {
                    int quantityToOrder = product.ReorderQuantity > 0 ? product.ReorderQuantity : 10;
                    var orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

                    var newOrder = new ReorderOrder
                    {
                        OrderNumber = orderNumber,
                        ProductId = product.Id,
                        Product = product,
                        Quantity = quantityToOrder,
                        EstimatedTotalCost = quantityToOrder * product.UnitPrice,
                        CreatedAt = DateTime.UtcNow,
                        Status = ReorderStatus.Pending,
                        TriggerReason = $"Automatikus szabály: Készlet ({currentStock} {product.Unit}) < Küszöb ({product.MinimumStockLevel} {product.Unit}).",
                        Notes = $"Beszállító: {product.SupplierName ?? "Nincs megadva"} | Email: {product.SupplierEmail ?? "kundavid16@gmail.com"}"
                    };

                    _context.ReorderOrders.Add(newOrder);
                    generatedCount++;

                    // Email értesítő küldése kundavid16@gmail.com részére
                    try
                    {
                        await _emailService.SendAutoReorderCreatedNotificationAsync(newOrder, product, currentStock);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Nem sikerült e-mail értesítőt küldeni az utánrendelésről");
                    }
                }
            }
        }

        if (generatedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return generatedCount;
    }

    public async Task<ReorderOrder> ApproveOrderAsync(int orderId)
    {
        var order = await _context.ReorderOrders
            .Include(ro => ro.Product)
            .FirstOrDefaultAsync(ro => ro.Id == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Nem található megrendelés {orderId} azonosítóval.");
        }

        if (order.Status != ReorderStatus.Pending)
        {
            throw new InvalidOperationException($"Csak függőben lévő (Pending) megrendelés hagyható jóvá. Jelenlegi státusz: {order.Status}");
        }

        order.Status = ReorderStatus.Ordered;
        order.OrderedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (order.Product != null)
        {
            try
            {
                await _emailService.SendOrderApprovedNotificationAsync(order, order.Product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nem sikerült e-mail értesítőt küldeni a jóváhagyásról");
            }
        }

        return order;
    }

    public async Task<ReorderOrder> CancelOrderAsync(int orderId, string? reason = null)
    {
        var order = await _context.ReorderOrders.FindAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Nem található megrendelés {orderId} azonosítóval.");
        }

        if (order.Status == ReorderStatus.Received)
        {
            throw new InvalidOperationException("Már bevételezett megrendelést nem lehet törölni.");
        }

        order.Status = ReorderStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            order.Notes = (order.Notes != null ? order.Notes + " | " : "") + "Törlés oka: " + reason;
        }

        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<ReorderOrder> CompleteOrderAsync(int orderId)
    {
        var order = await _context.ReorderOrders.FindAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Nem található megrendelés {orderId} azonosítóval.");
        }

        order.Status = ReorderStatus.Received;
        order.FulfilledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<int> GetPendingOrdersCountAsync()
    {
        return await _context.ReorderOrders
            .CountAsync(ro => ro.Status == ReorderStatus.Pending);
    }
}
