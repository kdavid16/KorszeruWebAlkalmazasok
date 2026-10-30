using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryManager.Web.ViewModels;

public class IssueStockViewModel
{
    [Required(ErrorMessage = "A termék kiválasztása kötelező.")]
    [Display(Name = "Termék")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "A kiadni kívánt mennyiség megadása kötelező.")]
    [Range(1, 1000000, ErrorMessage = "A kiadni kívánt mennyiségnek legalább 1-nek kell lennie.")]
    [Display(Name = "Kiadandó mennyiség")]
    public int Quantity { get; set; } = 1;

    [StringLength(100, ErrorMessage = "A bizonylatszám legfeljebb 100 karakter lehet.")]
    [Display(Name = "Bizonylat / Megrendelésszám")]
    public string? ReferenceNumber { get; set; }

    [StringLength(500, ErrorMessage = "A megjegyzés legfeljebb 500 karakter lehet.")]
    [Display(Name = "Megjegyzés / Cél / Átvevő")]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem>? ProductOptions { get; set; }
}
