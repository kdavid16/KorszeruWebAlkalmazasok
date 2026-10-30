using System.Net;
using System.Net.Mail;
using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryManager.Web.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly InventoryDbContext _context;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        InventoryDbContext context,
        IOptions<EmailSettings> settings,
        ILogger<EmailNotificationService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public EmailSettings GetSettings() => _settings;

    public async Task<EmailNotificationResult> SendNotificationAsync(string recipient, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            recipient = _settings.RecipientEmail;
        }

        var log = new EmailNotificationLog
        {
            Recipient = recipient,
            Subject = subject,
            Body = htmlBody,
            SentAt = DateTime.UtcNow
        };

        EmailNotificationResult result;

        if (_settings.EnableRealSending)
        {
            var username = _settings.Username?.Trim() ?? string.Empty;
            // Automatikus tisztítás: a szóközök eltávolítása az alkalmazásjelszóból
            var cleanPassword = (_settings.Password ?? string.Empty).Replace(" ", "").Trim();

            // Ellenőrizzük, hogy megadta-e a hitelesítési adatokat
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cleanPassword))
            {
                var errorMsg = "A valódi küldés (EnableRealSending = true) be van kapcsolva, de az 'Username' vagy 'Password' mező üres az appsettings.json fájlban! Valós Gmail küldéshez szükség van egy küldő Gmail címre és egy Google Alkalmazásjelszóra.";
                log.IsSuccess = false;
                log.StatusMessage = errorMsg;
                _logger.LogWarning("[EMAIL ÉRTESÍTŐ] {Error}", errorMsg);

                result = EmailNotificationResult.Failed(errorMsg, wasReal: true);
            }
            else
            {
                try
                {
                    var fromAddress = username.Contains('@') ? username : _settings.SenderEmail;

                    using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                    {
                        EnableSsl = _settings.EnableSsl,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(username, cleanPassword),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = 15000
                    };

                    using var message = new MailMessage
                    {
                        From = new MailAddress(fromAddress, _settings.SenderName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };
                    message.To.Add(recipient);

                    await client.SendMailAsync(message);

                    log.IsSuccess = true;
                    log.StatusMessage = $"Sikeresen kiküldve valós SMTP szerveren ({_settings.SmtpServer}:{_settings.SmtpPort}) keresztül a(z) {recipient} címre.";
                    _logger.LogInformation("[EMAIL ÉRTESÍTŐ] Sikeres valós küldés: {Recipient} | {Subject}", recipient, subject);

                    result = EmailNotificationResult.Succeeded(log.StatusMessage, wasReal: true);
                }
                catch (SmtpException ex)
                {
                    var innerDetail = ex.InnerException != null ? $" (Részlet: {ex.InnerException.Message})" : "";
                    var errorMsg = $"SMTP hiba történt a levélküldéskor ({ex.StatusCode}): {ex.Message}{innerDetail}";

                    if (ex.Message.Contains("5.7.8") || ex.Message.Contains("Username and Password not accepted") || 
                        (ex.InnerException != null && ex.InnerException.Message.Contains("net_io_connectionclosed")))
                    {
                        errorMsg = "Google Hitelesítési Hiba (535 BadCredentials): A Google elutasította a felhasználónév / jelszó párost! Ez akkor fordul elő, ha: 1) A 16 betűs Alkalmazásjelszó nem ehhez a fiókhoz ('" + username + "') lett létrehozva, hanem a böngésződben megnyitott másik Google fiókhoz; vagy 2) Elgépelés történt az Alkalmazásjelszóban; vagy 3) Nincs bekapcsolva a 2-lépcsős azonosítás a fiókban.";
                    }

                    log.IsSuccess = false;
                    log.StatusMessage = errorMsg;
                    _logger.LogError(ex, "[EMAIL ÉRTESÍTŐ] SMTP küldési hiba: {Error}", errorMsg);

                    result = EmailNotificationResult.Failed(errorMsg, wasReal: true);
                }
                catch (Exception ex)
                {
                    var innerDetail = ex.InnerException != null ? $" (Részlet: {ex.InnerException.Message})" : "";
                    var errorMsg = $"Nem sikerült az e-mail elküldése: {ex.Message}{innerDetail}";
                    log.IsSuccess = false;
                    log.StatusMessage = errorMsg;
                    _logger.LogError(ex, "[EMAIL ÉRTESÍTŐ] Általános küldési hiba: {Error}", errorMsg);

                    result = EmailNotificationResult.Failed(errorMsg, wasReal: true);
                }
            }
        }
        else
        {
            // Fejlesztői / Szimulált mód
            log.IsSuccess = true;
            log.StatusMessage = "Rögzítve az értesítési naplóban (Fejlesztői mód - EnableRealSending: false)";
            _logger.LogInformation("[EMAIL ÉRTESÍTŐ] Szimulált kézbesítés: {Recipient} | {Subject}", recipient, subject);

            result = EmailNotificationResult.Succeeded(log.StatusMessage, wasReal: false);
        }

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<EmailNotificationResult> SendAutoReorderCreatedNotificationAsync(ReorderOrder order, Product product, int currentStock)
    {
        var subject = $"[InventoryManager] ⚠️ Automatikus Utánrendelés Értesítő: {product.Name} ({product.Sku})";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; padding: 20px;">
                <h2 style="color: #d9534f; margin-top: 0;">⚠️ Automatikus Utánrendelés Generálva!</h2>
                <p>Kedves <strong>{_settings.RecipientEmail}</strong> (Beszerző és Rendszeradminisztrátor)!</p>
                <p>Az InventoryManager raktárkezelő rendszer automatikus szabálya szerint a készlet a megadott küszöb alá esett.</p>
                <table style="width: 100%; border-collapse: collapse; margin: 15px 0;">
                    <tr style="background: #f8f9fa;"><td style="padding: 8px; border: 1px solid #eee;"><strong>Rendelésszám:</strong></td><td style="padding: 8px; border: 1px solid #eee;"><code>{order.OrderNumber}</code></td></tr>
                    <tr><td style="padding: 8px; border: 1px solid #eee;"><strong>Terméknév:</strong></td><td style="padding: 8px; border: 1px solid #eee;">{product.Name}</td></tr>
                    <tr style="background: #f8f9fa;"><td style="padding: 8px; border: 1px solid #eee;"><strong>Cikkszám (SKU):</strong></td><td style="padding: 8px; border: 1px solid #eee;">{product.Sku}</td></tr>
                    <tr><td style="padding: 8px; border: 1px solid #eee;"><strong>Aktuális készlet:</strong></td><td style="padding: 8px; border: 1px solid #eee; color: red; font-weight: bold;">{currentStock} {product.Unit}</td></tr>
                    <tr style="background: #f8f9fa;"><td style="padding: 8px; border: 1px solid #eee;"><strong>Minimális küszöb:</strong></td><td style="padding: 8px; border: 1px solid #eee;">{product.MinimumStockLevel} {product.Unit}</td></tr>
                    <tr><td style="padding: 8px; border: 1px solid #eee;"><strong>Utánrendelt tétel:</strong></td><td style="padding: 8px; border: 1px solid #eee; font-weight: bold; color: green;">+{order.Quantity} {product.Unit}</td></tr>
                    <tr style="background: #f8f9fa;"><td style="padding: 8px; border: 1px solid #eee;"><strong>Becsült összérték:</strong></td><td style="padding: 8px; border: 1px solid #eee;">{order.EstimatedTotalCost:N0} Ft</td></tr>
                    <tr><td style="padding: 8px; border: 1px solid #eee;"><strong>Elsődleges beszállító:</strong></td><td style="padding: 8px; border: 1px solid #eee;">{product.SupplierName ?? "Nincs megadva"} ({product.SupplierEmail ?? "-"})</td></tr>
                </table>
                <p style="color: #666; font-size: 13px;">Kérjük, lépjen be a rendszerbe és hagyja jóvá a megrendelést a beszállító felé!</p>
            </div>
            """;

        return await SendNotificationAsync(_settings.RecipientEmail, subject, body);
    }

    public async Task<EmailNotificationResult> SendOrderApprovedNotificationAsync(ReorderOrder order, Product product)
    {
        var subject = $"[InventoryManager] 🚚 Megrendelés Jóváhagyva a Beszállítónak: {order.OrderNumber}";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; padding: 20px;">
                <h2 style="color: #0275d8; margin-top: 0;">🚚 Beszerzési Megrendelés Jóváhagyva!</h2>
                <p>A(z) <strong>{order.OrderNumber}</strong> számú megrendelés jóvá lett hagyva és elküldve a beszállítónak ({product.SupplierName ?? "Beszállító"}).</p>
                <p><strong>Megrendelt termék:</strong> {product.Name} ({product.Sku})</p>
                <p><strong>Várható mennyiség:</strong> {order.Quantity} {product.Unit}</p>
                <p>A raktáros kollégák értesítést kapnak, amint a fizikai szállítmány megérkezik a raktárba.</p>
            </div>
            """;

        return await SendNotificationAsync(_settings.RecipientEmail, subject, body);
    }

    public async Task<EmailNotificationResult> SendStockReceivedNotificationAsync(Product product, int quantity, string? refNumber)
    {
        var subject = $"[InventoryManager] 📦 Új Árubevételezés Értesítő: {product.Name} (+{quantity} {product.Unit})";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; padding: 20px;">
                <h2 style="color: #5cb85c; margin-top: 0;">📦 Áru sikeresen bevételezve a raktárba!</h2>
                <p><strong>Termék:</strong> {product.Name} ({product.Sku})</p>
                <p><strong>Beérkezett mennyiség:</strong> +{quantity} {product.Unit}</p>
                <p><strong>Szállítólevél / Bizonylatszám:</strong> {refNumber ?? "Nincs megadva"}</p>
            </div>
            """;

        return await SendNotificationAsync(_settings.RecipientEmail, subject, body);
    }

    public async Task<IReadOnlyList<EmailNotificationLog>> GetRecentLogsAsync(int count = 50)
    {
        return await _context.EmailLogs
            .AsNoTracking()
            .OrderByDescending(e => e.SentAt)
            .Take(count)
            .ToListAsync();
    }
}
