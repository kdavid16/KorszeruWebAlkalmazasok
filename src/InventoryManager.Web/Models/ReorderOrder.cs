using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManager.Web.Models;

public class ReorderOrder
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Rendelésszám")]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    [Range(1, 1000000)]
    [Display(Name = "Rendelt mennyiség")]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Becsült érték")]
    public decimal EstimatedTotalCost { get; set; }

    [Display(Name = "Generálás dátuma")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Megrendelve ekkor")]
    public DateTime? OrderedAt { get; set; }

    [Display(Name = "Teljesítve ekkor")]
    public DateTime? FulfilledAt { get; set; }

    [Required]
    [Display(Name = "Státusz")]
    public ReorderStatus Status { get; set; } = ReorderStatus.Pending;

    [StringLength(250)]
    [Display(Name = "Kiváltó ok")]
    public string? TriggerReason { get; set; }

    [StringLength(500)]
    [Display(Name = "Megjegyzés")]
    public string? Notes { get; set; }
}
