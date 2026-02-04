using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public int getVehicleActive()
        {
            try
            {
                return _context.Vehicles.Count(p => p.Status == "Active" && !p.Deleted);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
    }
}
