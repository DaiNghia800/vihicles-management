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

        public async Task<IActionResult> Index()
        {
            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .OrderByDescending(t => t.DepartureTime)
                .ToListAsync();
            return View("~/Views/Admin/Trip/Index.cshtml", trips);
        }

        public IActionResult Create()
        {
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName");
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate");
            return View("~/Views/Admin/Trip/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trip trip)
        {
            if (ModelState.IsValid)
            {
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", trip.RouteId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", trip.VehicleId);
            return View("~/Views/Admin/Trip/Create.cshtml", trip);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", trip.RouteId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", trip.VehicleId);
            return View("~/Views/Admin/Trip/Edit.cshtml", trip);
        }

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
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", trip.RouteId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", trip.VehicleId);
            return View("~/Views/Admin/Trip/Edit.cshtml", trip);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(m => m.TripId == id);
            if (trip == null) return NotFound();

            return View("~/Views/Admin/Trip/Delete.cshtml", trip);
        }

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
    }
}