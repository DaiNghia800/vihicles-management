using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "User is required")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [Required(ErrorMessage = "License number is required")]
        [StringLength(50, ErrorMessage = "License number cannot exceed 50 characters")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License type is required")]
        [StringLength(20, ErrorMessage = "License type cannot exceed 20 characters")]
        public string LicenseType { get; set; } = string.Empty;

        [Required(ErrorMessage = "License expiry date is required")]
        public DateTime? LicenseExpiry { get; set; }

        [Required(ErrorMessage = "Years of experience is required")]
        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public int? VehicleAssignedId { get; set; }

        [ForeignKey("VehicleAssignedId")]
        public virtual Vehicle? VehicleAssigned { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for trips
        public virtual ICollection<Trip>? Trips { get; set; }

        // ✅ Computed property để lấy ảnh từ User
        [NotMapped]
        public string ProfileImage => User?.ImgUser ?? "https://res.cloudinary.com/dfeaar87r/image/upload/v1763101391/default-avatar_uek2f1.png";
    }
}