namespace InventoryManager.Web.ViewModels;

public class ProductStockSummaryViewModel
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = "db";
    public decimal UnitPrice { get; set; }
    public int CurrentStock { get; set; }
    public int MinimumStockLevel { get; set; }

    public decimal TotalValue => CurrentStock * UnitPrice;

    // Szabály: Utánrendelési jelzés / alacsony készlet szigorúan a megadott küszöb ALATT (<) keletkezik!
    public bool IsLowStock => CurrentStock > 0 && CurrentStock < MinimumStockLevel;
    public bool IsOutOfStock => CurrentStock <= 0;
}
