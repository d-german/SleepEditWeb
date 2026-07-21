using System.ComponentModel.DataAnnotations;

namespace SleepEditWeb.Models;

public sealed class AdminLoginViewModel
{
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }
}
