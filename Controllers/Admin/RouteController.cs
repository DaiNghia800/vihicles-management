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
            // 1. Xóa validate cho các danh sách lớn (quan hệ 1-n)
            ModelState.Remove("Trips");
            ModelState.Remove("RouteDetails");

            // 2. [FIX QUAN TRỌNG] Xóa lỗi validate cho từng dòng chi tiết
            // Phải dùng .ToList() để tạo bản sao danh sách Keys, tránh lỗi "Collection was modified"
            var keys = ModelState.Keys.ToList();

            foreach (var key in keys)
            {
                // Tìm các lỗi liên quan đến RouteDetails
                if (key.Contains("RouteDetails["))
                {
                    // Bỏ qua lỗi bắt buộc phải có object Route và Station (vì mình chỉ gửi ID)
                    // Lỗi "The value '' is invalid" thường do nó cố bind object rỗng
                    if (key.EndsWith(".Route") || key.EndsWith(".Station") || key.EndsWith(".StationId"))
                    {
                        // Chỉ xóa lỗi nếu thực sự StationId đã có giá trị (tức là đã chọn trạm)
                        // Nhưng để an toàn cho case này, ta xóa hết các lỗi binding object con
                        if (key.EndsWith(".Route") || key.EndsWith(".Station"))
                        {
                            ModelState.Remove(key);
                        }
                    }
                }
            }

            // 3. DEBUG: Nếu vẫn lỗi, đoạn này sẽ in tên trường bị lỗi ra cửa sổ Output của Visual Studio
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== DANH SÁCH LỖI VALIDATION ===");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        // Nhìn vào đây em sẽ biết chính xác trường nào đang bị lỗi
                        Console.WriteLine($"Key: {key} - Error: {error.ErrorMessage}");
                    }
                }
                Console.WriteLine("=================================");
            }

            if (ModelState.IsValid)
            {
                // 4. Logic gán ngược Route cho RouteDetails
                if (route.RouteDetails != null && route.RouteDetails.Count > 0)
                {
                    foreach (var detail in route.RouteDetails)
                    {
                        detail.Route = route;
                    }
                }

                _context.Add(route);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Load lại dropdown nếu lỗi
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
        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Public_Transport.Models.Entities.Route route)
        {
            if (id != route.RouteId) return NotFound();

            // 1. Xóa validate danh sách lớn
            ModelState.Remove("Trips");
            ModelState.Remove("RouteDetails");

            // 2. [FIX QUAN TRỌNG] Xóa lỗi validate chi tiết (Giống hệt bên Create)
            var keys = ModelState.Keys.ToList();
            foreach (var key in keys)
            {
                if (key.Contains("RouteDetails["))
                {
                    // Bỏ qua lỗi null object hoặc lỗi rỗng distance
                    if (key.EndsWith(".Route") ||
                        key.EndsWith(".Station") ||
                        key.EndsWith(".StationId") ||
                        key.EndsWith(".DistanceFromStart"))
                    {
                        ModelState.Remove(key);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 3. Lấy dữ liệu cũ từ DB (Bao gồm cả chi tiết trạm)
                    var routeInDb = await _context.Routes
                        .Include(r => r.RouteDetails)
                        .FirstOrDefaultAsync(r => r.RouteId == id);

                    if (routeInDb == null) return NotFound();

                    // 4. Update thông tin cơ bản
                    routeInDb.RouteName = route.RouteName;
                    routeInDb.Description = route.Description;
                    routeInDb.BasePrice = route.BasePrice;
                    routeInDb.TotalDistance = route.TotalDistance;

                    // 5. Update danh sách trạm (Chiến thuật: Xóa hết cũ -> Thêm mới)

                    // Xóa danh sách cũ trong DB
                    _context.RouteDetails.RemoveRange(routeInDb.RouteDetails);

                    // Thêm danh sách mới từ Form gửi lên
                    if (route.RouteDetails != null)
                    {
                        foreach (var detail in route.RouteDetails)
                        {
                            detail.RouteId = id; // Gán đúng ID của Route hiện tại
                            detail.Route = routeInDb; // Gán object để EF hiểu
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

            // DEBUG: In lỗi ra Output nếu vẫn không update được
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== EDIT VALIDATION ERRORS ===");
                foreach (var key in ModelState.Keys)
                {
                    foreach (var err in ModelState[key].Errors)
                        Console.WriteLine($"{key}: {err.ErrorMessage}");
                }
            }

            // Load lại dropdown nếu lỗi
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