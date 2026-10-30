using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManager.Web.Controllers;

[Authorize]
public class ReordersController : Controller
{
    private readonly IReorderService _reorderService;
    private readonly ILogger<ReordersController> _logger;

    public ReordersController(IReorderService reorderService, ILogger<ReordersController> logger)
    {
        _reorderService = reorderService;
        _logger = logger;
    }

    // GET: /Reorders
    public async Task<IActionResult> Index(ReorderStatus? status)
    {
        var orders = await _reorderService.GetAllOrdersAsync(status);
        ViewBag.SelectedStatus = status;
        ViewBag.PendingCount = await _reorderService.GetPendingOrdersCountAsync();

        return View(orders);
    }

    // POST: /Reorders/TriggerRules (Csak Beszerző futtathatja manuálisan is)
    [HttpPost]
    [Authorize(Roles = "Beszerzo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TriggerRules()
    {
        try
        {
            int generated = await _reorderService.CheckAndGenerateAutoReordersAsync();
            if (generated > 0)
            {
                TempData["SuccessMessage"] = $"A szabályok lefutottak: {generated} új automatikus utánrendelés lett legenerálva!";
            }
            else
            {
                TempData["InfoMessage"] = "A szabályok lefutottak: Minden termék készlete megfelelő, vagy már folyamatban van az utánrendelése.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hiba az utánrendelési szabályok futtatásakor");
            TempData["ErrorMessage"] = "Hiba történt az automatikus szabályok futtatásakor: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Reorders/Approve/5 (Csak Beszerző hagyhatja jóvá a beszállító felé)
    [HttpPost]
    [Authorize(Roles = "Beszerzo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var order = await _reorderService.ApproveOrderAsync(id);
            TempData["SuccessMessage"] = $"A(z) {order.OrderNumber} számú megrendelés jóváhagyva és elküldve a beszállítónak ({order.Product?.SupplierName ?? "Beszállító"})!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hiba a megrendelés jóváhagyásakor");
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Reorders/Cancel/5
    [HttpPost]
    [Authorize(Roles = "Beszerzo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        try
        {
            var order = await _reorderService.CancelOrderAsync(id, reason);
            TempData["SuccessMessage"] = $"A(z) {order.OrderNumber} számú megrendelés törölve lett.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hiba a megrendelés törlésekor");
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
