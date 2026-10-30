using InventoryManager.Web.Models;
using InventoryManager.Web.ViewModels;

namespace InventoryManager.Web.Services;

public interface IInventoryService
{
    Task<StockDashboardViewModel> GetDashboardSummaryAsync();
    Task<IReadOnlyList<ProductStockSummaryViewModel>> GetStockListAsync();
    Task<int> GetProductStockAsync(int productId);
    Task<StockMovement> RecordReceiptAsync(int productId, int quantity, string? referenceNumber, string? notes);
    Task<StockMovement> RecordIssueAsync(int productId, int quantity, string? referenceNumber, string? notes);
    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int? productId = null);
}
