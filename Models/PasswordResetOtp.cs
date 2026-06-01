using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models
{
    public class PasswordResetOtp
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        [Required]
        public string OtpHash { get; set; }

        public DateTime ExpireAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}