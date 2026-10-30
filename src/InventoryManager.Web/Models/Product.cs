using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManager.Web.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "A cikkszám megadása kötelező.")]
    [StringLength(50, ErrorMessage = "A cikkszám legfeljebb 50 karakter lehet.")]
    [Display(Name = "Cikkszám (SKU)")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "A terméknév megadása kötelező.")]
    [StringLength(100, ErrorMessage = "A terméknév legfeljebb 100 karakter lehet.")]
    [Display(Name = "Terméknév")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A leírás legfeljebb 500 karakter lehet.")]
    [Display(Name = "Leírás")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "A kategória legfeljebb 50 karakter lehet.")]
    [Display(Name = "Kategória")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "A mértékegység megadása kötelező.")]
    [StringLength(20)]
    [Display(Name = "Mértékegység")]
    public string Unit { get; set; } = "db";

    [Range(0, 100000000, ErrorMessage = "Az egységár nem lehet negatív.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Egységár (Ft)")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100000, ErrorMessage = "A minimális készletszint nem lehet negatív.")]
    [Display(Name = "Minimális készletszint")]
    public int MinimumStockLevel { get; set; } = 5;

    // Automatikus utánrendelési szabály mezők
    [Display(Name = "Automata utánrendelés engedélyezve")]
    public bool AutoReorderEnabled { get; set; } = true;

    [Range(1, 100000, ErrorMessage = "Az utánrendelési mennyiségnek legalább 1-nek kell lennie.")]
    [Display(Name = "Utánrendelési mennyiség")]
    public int ReorderQuantity { get; set; } = 15;

    [StringLength(100)]
    [Display(Name = "Elsődleges beszállító")]
    public string? SupplierName { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Beszállító e-mail")]
    public string? SupplierEmail { get; set; }

    [Display(Name = "Létrehozva")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<ReorderOrder> ReorderOrders { get; set; } = new List<ReorderOrder>();
}
