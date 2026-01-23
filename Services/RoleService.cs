using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Services
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _context;

        public RoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Roles> GetAllRole()
        {
            try
            {
                return _context.Roles.Where(p => !p.Deleted).ToList();
            }
            catch (Exception ex) {
                return new List<Roles>();
            }
        }
    }
}
