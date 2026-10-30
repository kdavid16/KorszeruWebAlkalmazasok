namespace InventoryManager.Web.ViewModels;

public class StockDashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalItemsInStock { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public IReadOnlyList<ProductStockSummaryViewModel> Items { get; set; } = new List<ProductStockSummaryViewModel>();
}
