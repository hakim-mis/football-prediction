using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Verified Email Address")]
        public string Email { get; set; }
    }
}