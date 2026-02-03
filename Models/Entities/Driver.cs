using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "User là bắt buộc")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [Required(ErrorMessage = "Số giấy phép là bắt buộc")]
        [StringLength(50, ErrorMessage = "Số giấy phép không được vượt quá 50 ký tự")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại giấy phép là bắt buộc")]
        [StringLength(20, ErrorMessage = "Loại giấy phép không được vượt quá 20 ký tự")]
        public string LicenseType { get; set; } = string.Empty;

        public DateTime? LicenseExpiry { get; set; }

        [Required(ErrorMessage = "Số năm kinh nghiệm là bắt buộc")]
        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm phải từ 0 đến 50")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public int? VehicleAssignedId { get; set; }

        [ForeignKey("VehicleAssignedId")]
        public virtual Vehicle? VehicleAssigned { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for trips
        public virtual ICollection<Trip>? Trips { get; set; }
    }
}