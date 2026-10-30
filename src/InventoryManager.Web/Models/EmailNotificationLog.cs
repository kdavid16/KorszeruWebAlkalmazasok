using System.ComponentModel.DataAnnotations;

namespace InventoryManager.Web.Models;

public class EmailNotificationLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Címzett")]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Tárgy")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Üzenet törzse")]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Küldés időpontja")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Sikeres")]
    public bool IsSuccess { get; set; } = true;

    [StringLength(100)]
    [Display(Name = "Státusz")]
    public string StatusMessage { get; set; } = "Elküldve";
}
