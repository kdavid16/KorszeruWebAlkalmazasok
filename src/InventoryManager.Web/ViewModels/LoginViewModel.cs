using System.ComponentModel.DataAnnotations;

namespace InventoryManager.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "A felhasználónév megadása kötelező.")]
    [Display(Name = "Felhasználónév")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "A jelszó megadása kötelező.")]
    [DataType(DataType.Password)]
    [Display(Name = "Jelszó")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
