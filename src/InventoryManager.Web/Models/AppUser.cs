using System.ComponentModel.DataAnnotations;

namespace InventoryManager.Web.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Role { get; set; } = string.Empty; // "Raktaros" vagy "Beszerzo"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
