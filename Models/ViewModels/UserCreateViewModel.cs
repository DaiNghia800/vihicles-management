using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.ViewModels
{
    public class UserCreateViewModel
    {
        public int Uid { get; set; }
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s']+$", ErrorMessage = "Full name must not contain numbers or special characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Display(Name = "Phone number")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Invalid phone number. It must start with 0 and contain 10 digits.")]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$",
            ErrorMessage = "Password must be at least 6 characters long and include one uppercase letter, one lowercase letter, one number, and one special character.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "You must select a role for the user.")]
        public int RoleUid { get; set; }

        public IFormFile? ImgFile { get; set; }
        public string? ImgUser { get; set; }
    }
}
