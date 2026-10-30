using InventoryManager.Web.Models;

namespace InventoryManager.Web.Services;

public interface IEmailNotificationService
{
    Task<EmailNotificationResult> SendNotificationAsync(string recipient, string subject, string htmlBody);
    Task<EmailNotificationResult> SendAutoReorderCreatedNotificationAsync(ReorderOrder order, Product product, int currentStock);
    Task<EmailNotificationResult> SendOrderApprovedNotificationAsync(ReorderOrder order, Product product);
    Task<EmailNotificationResult> SendStockReceivedNotificationAsync(Product product, int quantity, string? refNumber);
    Task<IReadOnlyList<EmailNotificationLog>> GetRecentLogsAsync(int count = 50);
    EmailSettings GetSettings();
}
