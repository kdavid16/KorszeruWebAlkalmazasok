using InventoryManager.Web.Services;
using InventoryManager.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryManager.Web.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService, 
        IProductService productService,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _logger = logger;
    }

    // GET: /Inventory or /
    public async Task<IActionResult> Index()
    {
        var dashboard = await _inventoryService.GetDashboardSummaryAsync();
        return View(dashboard);
    }

    // GET: /Inventory/Receive - Csak Raktáros rögzíthet beérkezést
    [Authorize(Roles = "Raktaros")]
    public async Task<IActionResult> Receive(int? productId)
    {
        var products = await _productService.GetAllAsync();
        var model = new ReceiveStockViewModel
        {
            ProductId = productId ?? (products.FirstOrDefault()?.Id ?? 0),
            ProductOptions = products.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Sku} - {p.Name} ({p.Unit})"
            })
        };

        return View(model);
    }

    // POST: /Inventory/Receive
    [HttpPost]
    [Authorize(Roles = "Raktaros")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(ReceiveStockViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProductOptionsAsync(model);
            return View(model);
        }

        try
        {
            var movement = await _inventoryService.RecordReceiptAsync(
                model.ProductId, 
                model.Quantity, 
                model.ReferenceNumber, 
                model.Notes);

            TempData["SuccessMessage"] = $"Sikeres beérkezés rögzítve! Mennyiség: {movement.Quantity} db.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hiba a beérkezés rögzítésekor");
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateProductOptionsAsync(model);
            return View(model);
        }
    }

    // GET: /Inventory/Issue - Csak Raktáros adhat ki árut
    [Authorize(Roles = "Raktaros")]
    public async Task<IActionResult> Issue(int? productId)
    {
        var products = await _productService.GetAllAsync();
        var model = new IssueStockViewModel
        {
            ProductId = productId ?? (products.FirstOrDefault()?.Id ?? 0),
            ProductOptions = products.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Sku} - {p.Name} ({p.Unit})"
            })
        };

        return View(model);
    }

    // POST: /Inventory/Issue
    [HttpPost]
    [Authorize(Roles = "Raktaros")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(IssueStockViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProductOptionsAsync(model);
            return View(model);
        }

        try
        {
            var movement = await _inventoryService.RecordIssueAsync(
                model.ProductId, 
                model.Quantity, 
                model.ReferenceNumber, 
                model.Notes);

            TempData["SuccessMessage"] = $"Sikeres kiadás rögzítve! Mennyiség: {movement.Quantity} db.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateProductOptionsAsync(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hiba a kiadás rögzítésekor");
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateProductOptionsAsync(model);
            return View(model);
        }
    }

    // GET: /Inventory/Movements
    public async Task<IActionResult> Movements(int? productId)
    {
        var movements = await _inventoryService.GetMovementsAsync(productId);
        var products = await _productService.GetAllAsync();

        ViewBag.Products = products.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = $"{p.Sku} - {p.Name}",
            Selected = productId.HasValue && p.Id == productId.Value
        });

        ViewBag.SelectedProductId = productId;

        return View(movements);
    }

    private async Task PopulateProductOptionsAsync(ReceiveStockViewModel model)
    {
        var products = await _productService.GetAllAsync();
        model.ProductOptions = products.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = $"{p.Sku} - {p.Name} ({p.Unit})"
        });
    }

    private async Task PopulateProductOptionsAsync(IssueStockViewModel model)
    {
        var products = await _productService.GetAllAsync();
        model.ProductOptions = products.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = $"{p.Sku} - {p.Name} ({p.Unit})"
        });
    }
}
