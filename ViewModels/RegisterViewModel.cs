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
    [Display(Name = "Mobile Number")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please input designation."), StringLength(50)]
    [Display(Name = "Designation")]
    public string Designation { get; set; }

    [Required(ErrorMessage = "Please input department."), StringLength(50)]
    [Display(Name = "Department")]
    public string? Department { get; set; }

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Profile Photo")]
    public IFormFile? ProfilePhoto { get; set; }
}
