using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.Entities
{
    public class Roles
    {
        public int Uid { get; set; }

        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s']+$", ErrorMessage = "Tên vai trò không được chứa ký tự đặc biệt")]
        public string RoleName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool Deleted { get; set; }
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        public ICollection<Users> Users { get; set; } = new List<Users>();
    }
}
