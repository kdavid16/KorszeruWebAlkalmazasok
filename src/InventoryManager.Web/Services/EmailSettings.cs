namespace InventoryManager.Web.Services;

public class EmailSettings
{
    public string SenderName { get; set; } = "InventoryManager Rendszer";
    public string SenderEmail { get; set; } = "noreply@inventorymanager.local";
    public string RecipientEmail { get; set; } = "kundavid16@gmail.com";
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableRealSending { get; set; } = false;
}
