using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Public_Transport.Models.EF;
using Public_Transport.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Public_Transport.Controllers.Client
{
    [Route("trip")]
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TripController> _logger;

        public TripController(ApplicationDbContext context, ILogger<TripController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /trip
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /trip/details/{id}
        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                    .ThenInclude(d => d.User)
                .Include(t => t.Route)
                    .ThenInclude(r => r.RouteDetails.OrderBy(rd => rd.OrderIndex))
                        .ThenInclude(rd => rd.Station)
                .FirstOrDefaultAsync(m => m.TripId == id);

            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        // ✅ API: Search và Pagination cho Trips
        [HttpGet("api/search")]
        public async Task<IActionResult> SearchTrips(
            [FromQuery] string search = null,
            [FromQuery] string status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 9)
        {
            try
            {
                var query = _context.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                        .ThenInclude(d => d.User)
                    .Where(t => t.Status != "Cancelled") // Không hiển thị chuyến bị hủy
                    .AsQueryable();

                // ✅ Search theo Route Name hoặc Description
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower().Trim();
                    query = query.Where(t => 
                        t.Route.RouteName.ToLower().Contains(search) ||
                        t.Route.Description.ToLower().Contains(search) ||
                        t.Vehicle.LicensePlate.ToLower().Contains(search));
                }

                // ✅ Filter theo Status
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    query = query.Where(t => t.Status == status);
                }

                // ✅ Filter theo Date Range
                if (fromDate.HasValue)
                {
                    query = query.Where(t => t.DepartureTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(t => t.DepartureTime <= endOfDay);
                }

                // ✅ Filter theo Price Range
                if (minPrice.HasValue)
                {
                    query = query.Where(t => t.Route.BasePrice >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(t => t.Route.BasePrice <= maxPrice.Value);
                }

                // ✅ Sắp xếp: Chuyến gần nhất trước
                query = query.OrderBy(t => t.DepartureTime);

                // ✅ Đếm tổng số trips
                var totalItems = await query.CountAsync();

                // ✅ Lấy trips theo trang
                var trips = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        tripId = t.TripId,
                        routeName = t.Route.RouteName,
                        routeDescription = t.Route.Description,
                        departureTime = t.DepartureTime,
                        arrivalTime = t.ArrivalTime,
                        basePrice = t.Route.BasePrice,
                        totalDistance = t.Route.TotalDistance,
                        status = t.Status,
                        vehicleType = t.Vehicle != null ? t.Vehicle.VehicleType : "N/A",
                        vehicleCapacity = t.Vehicle != null ? t.Vehicle.SeatCapacity : 0,
                        licensePlate = t.Vehicle != null ? t.Vehicle.LicensePlate : "N/A",
                        driverName = t.Driver != null && t.Driver.User != null ? t.Driver.User.FullName : "Not Assigned",
                        thumbnail = t.Thumbnail,
                        // ✅ Tính số chỗ còn trống
                        availableSeats = t.Vehicle != null ? 
                            t.Vehicle.SeatCapacity - _context.Tickets
                                .Count(tk => tk.TripId == t.TripId && 
                                            (tk.Status == "Booked" || tk.Status == "Paid"))
                            : 0
                    })
                    .ToListAsync();

                // ✅ Trả về kèm pagination info
                return Ok(new
                {
                    trips,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching trips");
                return StatusCode(500, new 
                { 
                    message = "Error loading trips", 
                    error = ex.Message 
                });
            }
        }
    }
}