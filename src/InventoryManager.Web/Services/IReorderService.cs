using InventoryManager.Web.Models;

namespace InventoryManager.Web.Services;

public interface IReorderService
{
    Task<IReadOnlyList<ReorderOrder>> GetAllOrdersAsync(ReorderStatus? status = null);
    Task<ReorderOrder?> GetOrderByIdAsync(int id);
    Task<int> CheckAndGenerateAutoReordersAsync();
    Task<ReorderOrder> ApproveOrderAsync(int orderId);
    Task<ReorderOrder> CancelOrderAsync(int orderId, string? reason = null);
    Task<ReorderOrder> CompleteOrderAsync(int orderId);
    Task<int> GetPendingOrdersCountAsync();
}
