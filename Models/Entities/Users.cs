using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class Users
    {
        public int Uid { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        public string ImgUser { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        // ✅ FIX: BỎ [Required] vì DateOfBirth là nullable và không bắt buộc khi edit
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        // ✅ FIX: BỎ validation cho RoleUid vì nó được handle riêng
        public int RoleUid { get; set; }

        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public bool Deleted { get; set; }

        public Roles Role { get; set; } = null!;

        [NotMapped]
        public bool IsAdult
        {
            get
            {
                if (!DateOfBirth.HasValue) return false;
                var age = DateTime.Today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                return age >= 18;
            }
        }
    }
}