using System.ComponentModel.DataAnnotations;

namespace InventoryManager.Web.Models;

public class StockMovement
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Termék")]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    [Display(Name = "Mozgástípus")]
    public MovementType Type { get; set; }

    [Required]
    [Range(1, 1000000, ErrorMessage = "A mozgásmennyiségnek legalább 1-nek kell lennie.")]
    [Display(Name = "Mennyiség")]
    public int Quantity { get; set; }

    [Display(Name = "Dátum")]
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    [StringLength(100, ErrorMessage = "A hivatkozási szám legfeljebb 100 karakter lehet.")]
    [Display(Name = "Bizonylat / Szállítólevél")]
    public string? ReferenceNumber { get; set; }

    [StringLength(500, ErrorMessage = "A megjegyzés legfeljebb 500 karakter lehet.")]
    [Display(Name = "Megjegyzés")]
    public string? Notes { get; set; }
}
