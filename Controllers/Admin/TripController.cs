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
        public async Task<IActionResult> Index(string? error = null)
        {
            // Show error message if redirected from Delete action
            if (!string.IsNullOrEmpty(error))
            {
                ViewData["Error"] = error;
            }

            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .OrderByDescending(t => t.DepartureTime)
                .ToListAsync();

            // --- AUTO-UPDATE STATUS LOGIC ---
            bool hasChanges = false;
            var now = DateTime.Now;

            foreach (var trip in trips)
            {
                // Only process trips that are not Cancelled or Completed
                if (trip.Status != "Cancelled" && trip.Status != "Completed")
                {
                    // Case 1: Time to depart -> Set to Running
                    if (trip.DepartureTime <= now && trip.ArrivalTime > now && trip.Status != "Running")
                    {
                        trip.Status = "Running";
                        _context.Entry(trip).State = EntityState.Modified;
                        hasChanges = true;
                    }
                    // Case 2: Arrival time passed -> Set to Completed
                    else if (trip.ArrivalTime <= now && trip.Status != "Completed")
                    {
                        trip.Status = "Completed";
                        _context.Entry(trip).State = EntityState.Modified;
                        hasChanges = true;
                    }
                }
            }

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
            // 1. Default Status
            trip.Status = "Scheduled";
            ModelState.Remove("Status");

            // 2. Validate: Departure time must be in the future
            if (trip.DepartureTime <= DateTime.Now)
            {
                ModelState.AddModelError("DepartureTime", "The departure time must be later than the current time!");
            }

            // 3. Validate: Arrival time must be after Departure time
            if (trip.ArrivalTime <= trip.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "The estimated arrival time must be after the departure time!");
            }

            // 4. Remove Validation for Navigation Properties
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
            if (trip.Status == "Running" || trip.Status == "Completed")
            {
                return RedirectToAction(nameof(Index), new { error = "Cannot edit a trip that is currently Running or Completed!" });
            }

            LoadViewData(trip);
            return View("~/Views/Admin/Trip/Edit.cshtml", trip);
        }

        // POST: admin/trip/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trip trip)
        {
            if (id != trip.TripId) return NotFound();

            // 1. Validate Time Logic (Same as Create)
            if (trip.ArrivalTime <= trip.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "The estimated arrival time must be after the departure time!");
            }

            // 2. [IMPORTANT] Remove Validation for Navigation Properties
            ModelState.Remove("Route");
            ModelState.Remove("Vehicle");
            ModelState.Remove("Driver");

            // Note: We don't remove "Status" here because Edit form might allow changing Status
            // If the Edit form DOES NOT have a Status field, you must uncomment the line below:
            // ModelState.Remove("Status"); 

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the trip
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

            // Logic: Prevent deleting active trips
            if (trip.Status == "Running" || trip.Status == "Completed")
            {
                // Pass error via Query String (to be displayed in Index)
                return RedirectToAction(nameof(Index), new { error = "Cannot delete a trip that is currently Running or Completed!" });
            }

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
                // Double check logic before deleting
                if (trip.Status == "Running" || trip.Status == "Completed")
                {
                    return RedirectToAction(nameof(Index), new { error = "Cannot delete a trip that is currently Running or Completed!" });
                }

                _context.Trips.Remove(trip);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper to load dropdowns
        private void LoadViewData(Trip? trip = null)
        {
            // Load Route
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", trip?.RouteId);

            // Load Vehicle
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", trip?.VehicleId);

            // Load Driver (Custom display: Name + License)
            var drivers = _context.Drivers.Include(d => d.User)
                .Select(d => new {
                    d.DriverId,
                    DisplayName = d.User.FullName + " (" + d.LicenseNumber + ")"
                }).ToList();

            ViewData["DriverId"] = new SelectList(drivers, "DriverId", "DisplayName", trip?.DriverId);
        }
    }
}