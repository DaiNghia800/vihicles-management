using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.ViewModels
{
    public class ProfileUpdateViewModel
    {
        public int Uid { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s']+$", ErrorMessage = "Full name cannot contain numbers or special characters.")]
        public string? FullName { get; set; }

        [Display(Name = "Phone number")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Invalid phone number. It must start with 0 and contain 10 digits.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        public string? Address { get; set; }

        public IFormFile? Photo { get; set; }

        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$",
        ErrorMessage = "Password must be at least 6 characters with 1 uppercase, 1 lowercase, 1 number and 1 special character.")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}