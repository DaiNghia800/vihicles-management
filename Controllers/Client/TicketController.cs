using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using System.Security.Claims;

namespace Public_Transport.Controllers.Client
{
    [Route("my-tickets")]
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketController> _logger;

        public TicketController(ApplicationDbContext context, ILogger<TicketController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // View chính: My Tickets
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Ticket/MyTickets.cshtml");
        }

        // API: Lấy danh sách tickets của user hiện tại
        // Update GetMyTickets để include payment info đầy đủ hơn
        [HttpGet("api/my-tickets")]
        public async Task<IActionResult> GetMyTickets(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string status = null)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var query = _context.Tickets
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Route)
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Vehicle)
                    .Include(t => t.Payment) // ✅ Đảm bảo include payment
                    .Where(t => t.UserId == userId)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    query = query.Where(t => t.Status == status);
                }

                var totalItems = await query.CountAsync();

                var tickets = await query
                    .OrderByDescending(t => t.BookingDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.TicketId,
                        t.Price,
                        t.Status,
                        t.BookingDate,
                        Trip = new
                        {
                            t.Trip.TripId,
                            RouteName = t.Trip.Route.RouteName,
                            t.Trip.DepartureTime,
                            t.Trip.ArrivalTime,
                            VehicleType = t.Trip.Vehicle != null ? t.Trip.Vehicle.VehicleType : "N/A",
                            LicensePlate = t.Trip.Vehicle != null ? t.Trip.Vehicle.LicensePlate : "N/A"
                        },
                        Payment = t.Payment != null ? new
                        {
                            t.Payment.PaymentId,
                            t.Payment.Amount,
                            t.Payment.PaymentMethod,
                            t.Payment.Status,
                            t.Payment.TransactionRef,
                            t.Payment.PaymentDate
                        } : null,
                        // ✅ Thêm flag để check xem có thể thanh toán không
                        CanPay = t.Status == "Booked" && t.Trip.DepartureTime > DateTime.Now
                    })
                    .ToListAsync();

                return Ok(new
                {
                    tickets,
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
                _logger.LogError(ex, "Error getting user tickets");
                return StatusCode(500, new { message = "Error loading tickets", error = ex.Message });
            }
        }

        // API: Lấy chi tiết một ticket
        [HttpGet("api/detail/{ticketId}")]
        public async Task<IActionResult> GetTicketDetail(int ticketId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var ticket = await _context.Tickets
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Route)
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Vehicle)
                    .Include(t => t.Payment)
                    .Include(t => t.User)
                    .Where(t => t.TicketId == ticketId && t.UserId == userId)
                    .Select(t => new
                    {
                        t.TicketId,
                        t.Price,
                        t.Status,
                        t.BookingDate,
                        User = new
                        {
                            t.User.FullName,
                            t.User.Email,
                            t.User.PhoneNumber
                        },
                        Trip = new
                        {
                            t.Trip.TripId,
                            RouteName = t.Trip.Route.RouteName,
                            RouteDescription = t.Trip.Route.Description,
                            t.Trip.DepartureTime,
                            t.Trip.ArrivalTime,
                            t.Trip.Status,
                            Vehicle = t.Trip.Vehicle != null ? new
                            {
                                t.Trip.Vehicle.VehicleType,
                                t.Trip.Vehicle.LicensePlate
                            } : null
                        },
                        Payment = t.Payment != null ? new
                        {
                            t.Payment.PaymentId,
                            t.Payment.Amount,
                            t.Payment.PaymentMethod,
                            t.Payment.Status,
                            t.Payment.TransactionRef,
                            t.Payment.PaymentDate
                        } : null
                    })
                    .FirstOrDefaultAsync();

                if (ticket == null)
                {
                    return NotFound(new { message = "Ticket not found" });
                }

                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ticket detail for TicketId: {TicketId}", ticketId);
                return StatusCode(500, new { message = "Error loading ticket detail", error = ex.Message });
            }
        }

        // API: Hủy ticket
        [HttpPut("api/cancel/{ticketId}")]
        public async Task<IActionResult> CancelTicket(int ticketId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var ticket = await _context.Tickets
                    .Include(t => t.Trip)
                    .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.UserId == userId);

                if (ticket == null)
                {
                    return NotFound(new { message = "Ticket not found" });
                }

                // Kiểm tra xem có thể hủy không (ví dụ: chỉ hủy nếu chưa đến giờ khởi hành)
                if (ticket.Trip.DepartureTime <= DateTime.Now)
                {
                    return BadRequest(new { message = "Cannot cancel ticket after departure time" });
                }

                if (ticket.Status == "Cancelled")
                {
                    return BadRequest(new { message = "Ticket already cancelled" });
                }

                ticket.Status = "Cancelled";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Ticket cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling ticket {TicketId}", ticketId);
                return StatusCode(500, new { message = "Error cancelling ticket", error = ex.Message });
            }
        }
    }
}