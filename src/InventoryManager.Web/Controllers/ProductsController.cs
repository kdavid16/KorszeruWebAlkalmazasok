using InventoryManager.Web.Models;
using InventoryManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManager.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService, 
        IInventoryService inventoryService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    // GET: /Products
    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();
        return View(products);
    }

    // GET: /Products/Create - Csak a Beszerző vehet fel új terméket
    [Authorize(Roles = "Beszerzo")]
    public IActionResult Create()
    {
        return View(new Product());
    }

    // POST: /Products/Create
    [HttpPost]
    [Authorize(Roles = "Beszerzo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        try
        {
            await _productService.CreateAsync(product);
            TempData["SuccessMessage"] = $"A(z) '{product.Name}' ({product.Sku}) termék sikeresen rögzítve lett!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Sku", ex.Message);
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hiba a termék létrehozásakor");
            ModelState.AddModelError(string.Empty, "Nem várt hiba történt a mentés során: " + ex.Message);
            return View(product);
        }
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        ViewBag.CurrentStock = await _inventoryService.GetProductStockAsync(id);
        return View(product);
    }
}
