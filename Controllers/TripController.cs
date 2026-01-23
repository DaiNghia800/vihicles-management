using Microsoft.AspNetCore.Mvc;
using Public_Transport.Models.EF;
using Microsoft.EntityFrameworkCore;

namespace Public_Transport.Controllers
{
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TripController(ApplicationDbContext context)
        {
            _context = context;
        }

        // TRANG 1: Danh sách các chuyến (Ghép với HTML Routes của bạn)
        public async Task<IActionResult> Index()
        {
            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .OrderBy(t => t.DepartureTime) // Sắp xếp chuyến sắp chạy lên trước
                .ToListAsync();

            return View(trips);
        }

        // TRANG 2: Chi tiết chuyến đi (Ghép với HTML Route Detail)
        public async Task<IActionResult> Details(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Route)
                .ThenInclude(r => r.RouteDetails.OrderBy(rd => rd.OrderIndex)) // Lấy danh sách trạm
                .ThenInclude(rd => rd.Station)
                .FirstOrDefaultAsync(m => m.TripId == id);

            if (trip == null) return NotFound();

            return View(trip);
        }
    }
}
