using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryManager.Web.ViewModels;

public class ReceiveStockViewModel
{
    [Required(ErrorMessage = "A termék kiválasztása kötelező.")]
    [Display(Name = "Termék")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "A beérkezett mennyiség megadása kötelező.")]
    [Range(1, 1000000, ErrorMessage = "A beérkezett mennyiségnek legalább 1-nek kell lennie.")]
    [Display(Name = "Beérkezett mennyiség")]
    public int Quantity { get; set; } = 1;

    [StringLength(100, ErrorMessage = "A bizonylatszám legfeljebb 100 karakter lehet.")]
    [Display(Name = "Bizonylat / Szállítólevél száma")]
    public string? ReferenceNumber { get; set; }

    [StringLength(500, ErrorMessage = "A megjegyzés legfeljebb 500 karakter lehet.")]
    [Display(Name = "Megjegyzés / Beszállító adatai")]
    public string? Notes { get; set; }

    // Dropdown listának
    public IEnumerable<SelectListItem>? ProductOptions { get; set; }
}
