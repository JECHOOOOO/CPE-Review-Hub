using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(128)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(120), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [StringLength(120)] public string? University { get; set; }
    [StringLength(40), Display(Name = "Year level")] public string? YearLevel { get; set; }
    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class ProfileViewModel
{
    [Required, StringLength(120), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [StringLength(120)] public string? University { get; set; }
    [StringLength(40), Display(Name = "Year level")] public string? YearLevel { get; set; }
}
