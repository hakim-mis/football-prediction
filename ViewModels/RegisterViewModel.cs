using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "This field is required."), StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "This field is required."), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile Number is required.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "Mobile Number must be exactly 11 digits.")]
    [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "Mobile Number must be exactly 11 digits.")]
    [Display(Name = "Mobile Number (01XX)")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please input designation."), StringLength(50)]
    [Display(Name = "Designation")]
    public string Designation { get; set; }

    [Required(ErrorMessage = "Please input department."), StringLength(50)]
    [Display(Name = "Department & Company")]
    public string? Department { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(
    @"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
    ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one number, and one special character."
)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Password and confirm password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Profile Photo")]
    public IFormFile? ProfilePhoto { get; set; }
}
