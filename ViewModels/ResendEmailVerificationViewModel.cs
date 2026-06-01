using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.ViewModels
{
    public class ResendEmailVerificationViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Registered Email Address")]
        public string Email { get; set; }
    }
}