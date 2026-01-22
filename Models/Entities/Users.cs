namespace Public_Transport.Models.Entities
{
    public class Users
    {
        public int Uid { get; set; }
        public string FullName { get; set; }
        public string ImgUser { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string PasswordHash { get; set; }
        public int RoleUid { get; set; }
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool Deleted { get; set; }
        public Roles Role { get; set; }
    }
}
