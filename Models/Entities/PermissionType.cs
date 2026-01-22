namespace Public_Transport.Models.Entities
{
    public class PermissionType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}
