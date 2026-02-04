using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;

namespace Public_Transport.Controllers.Admin
{
    [Route("admin/station/[action]/{id?}")]
    public class StationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. List Stations
        // Updated to accept error message via Query String
        public async Task<IActionResult> Index(string? error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                ViewData["Error"] = error;
            }

            var stations = await _context.Stations
                .OrderBy(s => s.StationName)
                .ToListAsync();
            return View("~/Views/Admin/Station/Index.cshtml", stations);
        }

        // 2. Create - GET
        public IActionResult Create()
        {
            return View("~/Views/Admin/Station/Create.cshtml");
        }

        // 2. Create - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Station station)
        {
            // Remove navigation property validation to prevent IsValid = false errors
            ModelState.Remove("RouteDetails");

            if (ModelState.IsValid)
            {
                _context.Add(station);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/Station/Create.cshtml", station);
        }

        // 3. Edit - GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var station = await _context.Stations.FindAsync(id);
            if (station == null) return NotFound();

            return View("~/Views/Admin/Station/Edit.cshtml", station);
        }

        // 3. Edit - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Station station)
        {
            if (id != station.StationId) return NotFound();

            ModelState.Remove("RouteDetails");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(station);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Stations.Any(e => e.StationId == station.StationId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/Station/Edit.cshtml", station);
        }

        // 4.1 Delete - GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var station = await _context.Stations.FindAsync(id);
            if (station == null) return NotFound();

            // Logic: Check if station is assigned to any route
            var isUsed = await _context.RouteDetails.AnyAsync(rd => rd.StationId == id);
            if (isUsed)
            {
                // Redirect to Index with Error Message (passed via URL)
                return RedirectToAction(nameof(Index), new { error = "Cannot delete this station because it is currently assigned to a Route!" });
            }

            return View("~/Views/Admin/Station/Delete.cshtml", station);
        }

        // 4.2 Delete - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var station = await _context.Stations.FindAsync(id);
            if (station != null)
            {
                // Double Check Logic before deleting
                var isUsed = await _context.RouteDetails.AnyAsync(rd => rd.StationId == id);
                if (isUsed)
                {
                    return RedirectToAction(nameof(Index), new { error = "Cannot delete this station because it is currently assigned to a Route!" });
                }

                _context.Stations.Remove(station);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}