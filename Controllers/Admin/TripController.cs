using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;

namespace Public_Transport.Controllers.Admin
{
    [Route("admin/trip/[action]/{id?}")]
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TripController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: admin/trip
        public async Task<IActionResult> Index()
        {
            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .OrderByDescending(t => t.DepartureTime)
                .ToListAsync();

            // --- LOGIC TỰ ĐỘNG CẬP NHẬT STATUS ---
            bool hasChanges = false;
            var now = DateTime.Now;

            foreach (var trip in trips)
            {
                // Chỉ xử lý các chuyến chưa Hủy (Cancelled) và chưa Hoàn thành (Completed)
                if (trip.Status != "Cancelled" && trip.Status != "Completed")
                {
                    // Case 1: Đã đến giờ chạy nhưng chưa tới giờ đến -> Đổi thành Running
                    if (trip.DepartureTime <= now && trip.ArrivalTime > now && trip.Status != "Running")
                    {
                        trip.Status = "Running";
                        _context.Entry(trip).State = EntityState.Modified; // Đánh dấu để update DB
                        hasChanges = true;
                    }
                    // Case 2: Đã quá giờ đến -> Đổi thành Completed
                    else if (trip.ArrivalTime <= now && trip.Status != "Completed")
                    {
                        trip.Status = "Completed";
                        _context.Entry(trip).State = EntityState.Modified;
                        hasChanges = true;
                    }
                }
            }

            // Nếu có thay đổi thì lưu xuống Database luôn
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
            // ---------------------------------------

            return View("~/Views/Admin/Trip/Index.cshtml", trips);
        }

        // GET: admin/trip/create
        public IActionResult Create()
        {
            LoadViewData();
            return View("~/Views/Admin/Trip/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trip trip)
        {
            trip.Status = "Scheduled";
            ModelState.Remove("Status");
            // 1. LOGIC: Chặn tạo chuyến trong quá khứ
            if (trip.DepartureTime <= DateTime.Now)
            {
                ModelState.AddModelError("DepartureTime", "The departure time must be later than the current time!");
            }

            // 2. LOGIC: Giờ đến phải sau giờ đi
            if (trip.ArrivalTime <= trip.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "The estimated arrival time must be after the departure time!");
            }

            // 3. LOGIC: Tự động set Status khi tạo mới
            // Người dùng không cần chọn, hệ thống tự set là "Scheduled"
            trip.Status = "Scheduled";

            // Bỏ qua validate các object quan hệ (như bài trước)
            ModelState.Remove("Route");
            ModelState.Remove("Vehicle");
            ModelState.Remove("Driver");

            if (ModelState.IsValid)
            {
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadViewData(trip);
            return View("~/Views/Admin/Trip/Create.cshtml", trip);
        }

        // GET: admin/trip/edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            LoadViewData(trip);
            return View("~/Views/Admin/Trip/Edit.cshtml", trip);
        }

        // POST: admin/trip/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trip trip)
        {
            if (id != trip.TripId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Trips.Any(e => e.TripId == trip.TripId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            LoadViewData(trip);
            return View("~/Views/Admin/Trip/Edit.cshtml", trip);
        }

        // GET: admin/trip/delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.TripId == id);
            if (trip == null) return NotFound();

            return View("~/Views/Admin/Trip/Delete.cshtml", trip);
        }

        // POST: admin/trip/delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper để load dropdown cho gọn code
        private void LoadViewData(Trip? trip = null)
        {
            // Load Route
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", trip?.RouteId);

            // Load Vehicle
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", trip?.VehicleId);

            // Load Driver (Kỹ thuật: Chọn ra User.FullName để hiển thị)
            var drivers = _context.Drivers.Include(d => d.User)
                .Select(d => new {
                    d.DriverId,
                    DisplayName = d.User.FullName + " (" + d.LicenseNumber + ")"
                }).ToList();

            ViewData["DriverId"] = new SelectList(drivers, "DriverId", "DisplayName", trip?.DriverId);
        }
    }
}