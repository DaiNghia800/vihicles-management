using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.ViewModels
{
    public class UserEditViewModel
    {
        public int Uid { get; set; }

        [Required(ErrorMessage = "Full name cannot be left blank")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email cannot be blank")]
        [EmailAddress(ErrorMessage = "Email is not in correct format")]
        public string Email { get; set; }

        public string? Address { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must start with 0 and have 10 digits")]
        public string? PhoneNumber { get; set; }

        // Password không bắt buộc khi edit
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Confirm password does not match")]
        public string? ConfirmPassword { get; set; }

        //public IFormFile? ImgFile { get; set; }

        public string? ImgUser { get; set; } // Đường dẫn ảnh hiện tại

        [Required(ErrorMessage = "Please select a role")]
        public int RoleUid { get; set; }
    }
}