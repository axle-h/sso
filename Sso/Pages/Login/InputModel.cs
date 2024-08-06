using System.ComponentModel.DataAnnotations;

namespace Sso.Pages.Login;

public class InputModel
{
    [Required]
    public string? Username { get; set; }
    [Required]
    public string? Password { get; set; }
    public string? ReturnUrl { get; set; }
}