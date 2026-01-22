using System.Security;

namespace Public_Transport.Models.Entities
{
    public class Function
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool Deleted { get; set; }
        public string Status { get; set; }

        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}
