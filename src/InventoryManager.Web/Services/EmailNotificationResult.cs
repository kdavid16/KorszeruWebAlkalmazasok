namespace InventoryManager.Web.Services;

public class EmailNotificationResult
{
    public bool Success { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public bool WasRealSmtpAttempted { get; set; }

    public static EmailNotificationResult Succeeded(string message, bool wasReal = false) => new()
    {
        Success = true,
        StatusMessage = message,
        WasRealSmtpAttempted = wasReal
    };

    public static EmailNotificationResult Failed(string message, bool wasReal = false) => new()
    {
        Success = false,
        StatusMessage = message,
        WasRealSmtpAttempted = wasReal
    };
}
