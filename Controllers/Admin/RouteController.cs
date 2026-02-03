using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;

namespace Public_Transport.Controllers.Admin
{
    [Route("admin/route/[action]/{id?}")]
    public class RouteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RouteController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách tuyến
        public async Task<IActionResult> Index()
        {
            var routes = await _context.Routes
                .OrderBy(r => r.RouteName) // Sắp xếp theo tên cho dễ tìm
                .ToListAsync();
            return View("~/Views/Admin/Route/Index.cshtml", routes);
        }

        // 2. Tạo mới - GET
        public IActionResult Create()
        {
            return View("~/Views/Admin/Route/Create.cshtml");
        }

        // 2. Tạo mới - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Public_Transport.Models.Entities.Route route)
        {
            // Bỏ qua validate các object con (Trips, RouteDetails) vì lúc tạo chưa có
            ModelState.Remove("Trips");
            ModelState.Remove("RouteDetails");

            if (ModelState.IsValid)
            {
                _context.Add(route);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/Route/Create.cshtml", route);
        }

        // 3. Chỉnh sửa - GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();

            return View("~/Views/Admin/Route/Edit.cshtml", route);
        }

        // 3. Chỉnh sửa - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Public_Transport.Models.Entities.Route route)
        {
            if (id != route.RouteId) return NotFound();

            ModelState.Remove("Trips");
            ModelState.Remove("RouteDetails");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(route);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Routes.Any(e => e.RouteId == route.RouteId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/Route/Edit.cshtml", route);
        }

        // 4. Xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var route = await _context.Routes.FindAsync(id);

            // Logic: Nếu tuyến đã có chuyến chạy (Trip) thì không cho xóa để bảo toàn lịch sử
            var hasTrips = await _context.Trips.AnyAsync(t => t.RouteId == id);

            if (hasTrips)
            {
                TempData["Error"] = "Không thể xóa tuyến này vì đã có chuyến xe hoạt động!";
                return RedirectToAction(nameof(Index));
            }

            if (route != null)
            {
                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
 }
