using InventoryManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManager.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly IEmailNotificationService _emailService;

    public NotificationsController(IEmailNotificationService emailService)
    {
        _emailService = emailService;
    }

    // GET: /Notifications
    public async Task<IActionResult> Index()
    {
        var logs = await _emailService.GetRecentLogsAsync(100);
        ViewBag.Settings = _emailService.GetSettings();
        return View(logs);
    }

    // POST: /Notifications/SendTest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTest()
    {
        const string recipient = "kundavid16@gmail.com";
        var subject = "[InventoryManager] 🔔 Rendszer Teszt Értesítés";
        var body = $"""
            <div style="font-family: Arial, sans-serif; padding: 25px; border: 2px solid #28a745; border-radius: 8px; max-width: 600px; margin: 0 auto;">
                <h3 style="color: #28a745; margin-top: 0;">InventoryManager E-mail Értesítő Teszt</h3>
                <p>Kedves Dávid (<strong>{recipient}</strong>)!</p>
                <p>Ez egy valós teszt üzenet az <strong>InventoryManager</strong> raktárkészlet-kezelő alkalmazásból.</p>
                <div style="background: #f8f9fa; padding: 15px; border-left: 4px solid #28a745; margin: 15px 0;">
                    <strong>Teszt státusz:</strong> Az e-mail értesítő alrendszer és az SMTP kapcsolat sikeresen felépült!<br>
                    <strong>Küldés időpontja:</strong> {DateTime.UtcNow:yyyy.MM.dd. HH:mm:ss} UTC
                </div>
                <p style="color: #666; font-size: 13px;">Ezt a levelet az automatikus értesítési tesztfunkció generálta.</p>
            </div>
            """;

        var result = await _emailService.SendNotificationAsync(recipient, subject, body);

        if (result.Success)
        {
            if (result.WasRealSmtpAttempted)
            {
                TempData["SuccessMessage"] = $"A valódi e-mail sikeresen elküldve a(z) {recipient} címre a Google SMTP szerveren keresztül!";
            }
            else
            {
                TempData["InfoMessage"] = $"A teszt e-mail naplózva lett az adatbázisban (Fejlesztői mód - EnableRealSending: false). Ha valódi e-mailt szeretnél kapni a postafiókodba, töltsd ki a Gmail belépési adataidat az appsettings.json fájlban!";
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.StatusMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}
