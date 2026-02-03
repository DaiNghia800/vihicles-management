using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using System.Linq;
using System.Threading.Tasks;

namespace Public_Transport.Controllers
{
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TripController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // TRANG 1: DANH SÁCH CHUYẾN XE (Index)
        // URL mặc định: /Trip hoặc /Trip/Index
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var trips = await _context.Trips
                .Include(t => t.Route)   // Load thông tin tuyến
                .Include(t => t.Vehicle) // Load thông tin xe
                .OrderBy(t => t.DepartureTime) // Sắp xếp chuyến sắp chạy lên đầu
                .ToListAsync();

            // Trả về View: Views/Trip/Index.cshtml
            return View(trips);
        }

        // ==========================================
        // TRANG 2: CHI TIẾT CHUYẾN ĐI & BẢN ĐỒ (Details)
        // URL: /Trip/Details/5 (với 5 là TripId)
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Query "Thần thánh" để lấy full dữ liệu cho Map và Timeline
            var trip = await _context.Trips
                .Include(t => t.Vehicle) // Lấy xe
                //.Include(t => t.Driver)  // Lấy tài xế (nếu có)
                .Include(t => t.Route)   // Lấy tuyến đường
                                         // EAGER LOADING: Lấy chi tiết lộ trình -> Sắp xếp thứ tự -> Lấy trạm
                    .ThenInclude(r => r.RouteDetails.OrderBy(rd => rd.OrderIndex))
                        .ThenInclude(rd => rd.Station)
                .FirstOrDefaultAsync(m => m.TripId == id);

            if (trip == null)
            {
                return NotFound();
            }

            // Trả về View: Views/Trip/Details.cshtml
            return View(trip);
        }
    }
}