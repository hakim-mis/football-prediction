using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.ViewModels;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please input full name."), StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;


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

    public string? ExistingPhotoPath { get; set; }

    [Display(Name = "New Profile Photo")]
    public IFormFile? NewPhoto { get; set; }

    [Required(ErrorMessage = "This field is required."), StringLength(150)]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password), MinLength(8)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Confirm New Password")]
    public string? ConfirmNewPassword { get; set; }
}
