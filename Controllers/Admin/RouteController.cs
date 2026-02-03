using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities; // Ensure this namespace is correct

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

        // 1. Route List
        public async Task<IActionResult> Index()
        {
            var routes = await _context.Routes
                .OrderBy(r => r.RouteName) // Sort by name
                .ToListAsync();
            return View("~/Views/Admin/Route/Index.cshtml", routes);
        }

        // GET: Create
        public IActionResult Create()
        {
            // Load danh sách Trạm để hiển thị trong Dropdown
            ViewData["StationId"] = new SelectList(_context.Stations, "StationId", "StationName");
            return View("~/Views/Admin/Route/Create.cshtml");
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Public_Transport.Models.Entities.Route route)
        {
            // Logic: Chúng ta sẽ nhận list RouteDetails từ form luôn
            ModelState.Remove("Trips");

            // Xóa validate RouteDetails để tự check tay (nếu cần)
            // Vì MVC binding list đôi khi báo lỗi ảo
            ModelState.Remove("RouteDetails");

            if (ModelState.IsValid)
            {
                // Kiểm tra nếu người dùng có nhập danh sách trạm
                if (route.RouteDetails != null && route.RouteDetails.Count > 0)
                {
                    // Gán RouteId cho từng chi tiết (dù EF tự làm nhưng gán cho chắc)
                    foreach (var detail in route.RouteDetails)
                    {
                        detail.Route = route;
                    }
                }

                _context.Add(route);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, load lại dropdown trạm
            ViewData["StationId"] = new SelectList(_context.Stations, "StationId", "StationName");
            return View("~/Views/Admin/Route/Create.cshtml", route);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Load Route kèm theo RouteDetails và Station để hiện lại dữ liệu cũ
            var route = await _context.Routes
                .Include(r => r.RouteDetails)
                .ThenInclude(rd => rd.Station)
                .FirstOrDefaultAsync(m => m.RouteId == id);

            if (route == null) return NotFound();

            // Sắp xếp lại trạm theo thứ tự OrderIndex
            route.RouteDetails = route.RouteDetails.OrderBy(rd => rd.OrderIndex).ToList();

            ViewData["StationId"] = new SelectList(_context.Stations, "StationId", "StationName");
            return View("~/Views/Admin/Route/Edit.cshtml", route);
        }

        // POST: Edit
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
                    // 1. Update thông tin cơ bản của Route
                    var routeInDb = await _context.Routes
                        .Include(r => r.RouteDetails)
                        .FirstOrDefaultAsync(r => r.RouteId == id);

                    if (routeInDb == null) return NotFound();

                    routeInDb.RouteName = route.RouteName;
                    routeInDb.Description = route.Description;
                    routeInDb.BasePrice = route.BasePrice;
                    routeInDb.TotalDistance = route.TotalDistance;

                    // 2. Xử lý RouteDetails (Xóa cũ thêm mới - Cách đơn giản nhất)
                    // Xóa hết chi tiết cũ
                    _context.RouteDetails.RemoveRange(routeInDb.RouteDetails);

                    // Thêm chi tiết mới từ form
                    if (route.RouteDetails != null)
                    {
                        foreach (var detail in route.RouteDetails)
                        {
                            detail.RouteId = id; // Gán ID thủ công
                            _context.RouteDetails.Add(detail);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Routes.Any(e => e.RouteId == route.RouteId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["StationId"] = new SelectList(_context.Stations, "StationId", "StationName");
            return View("~/Views/Admin/Route/Edit.cshtml", route);
        }

        // 4.1 Delete - GET: Show confirmation page
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var route = await _context.Routes.FindAsync(id);

            if (route == null) return NotFound();

            // Logic: Prevent deletion if the route has associated trips
            var hasTrips = await _context.Trips.AnyAsync(t => t.RouteId == id);

            if (hasTrips)
            {
                TempData["Error"] = "Cannot delete this route because there are active trips associated with it!";
                return RedirectToAction(nameof(Index));
            }

            // Return the Delete View for confirmation
            return View("~/Views/Admin/Route/Delete.cshtml", route);
        }

        // 4.2 Delete - POST: Perform actual deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route != null)
            {
                // Double check for security
                var hasTrips = await _context.Trips.AnyAsync(t => t.RouteId == id);
                if (hasTrips)
                {
                    TempData["Error"] = "Cannot delete this route because there are active trips associated with it!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}