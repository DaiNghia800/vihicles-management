using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using System.Security.Claims;
using QRCoder; // Add this
using System.Drawing; // Add this
using System.Drawing.Imaging; // Add this

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
                    .Include(t => t.Payment)
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

        // API: Lấy chi tiết một ticket (Updated with QR code)
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
                    .FirstOrDefaultAsync();

                if (ticket == null)
                {
                    return NotFound(new { message = "Ticket not found" });
                }

                // Generate QR Code for Paid tickets
                string qrCodeBase64 = null;
                if (ticket.Status == "Paid")
                {
                    qrCodeBase64 = GenerateQRCode(ticketId);
                }

                return Ok(new
                {
                    ticket.TicketId,
                    ticket.Price,
                    ticket.Status,
                    ticket.BookingDate,
                    User = new
                    {
                        ticket.User.FullName,
                        ticket.User.Email,
                        ticket.User.PhoneNumber
                    },
                    Trip = new
                    {
                        ticket.Trip.TripId,
                        RouteName = ticket.Trip.Route.RouteName,
                        RouteDescription = ticket.Trip.Route.Description,
                        ticket.Trip.DepartureTime,
                        ticket.Trip.ArrivalTime,
                        ticket.Trip.Status,
                        Vehicle = ticket.Trip.Vehicle != null ? new
                        {
                            ticket.Trip.Vehicle.VehicleType,
                            ticket.Trip.Vehicle.LicensePlate
                        } : null
                    },
                    Payment = ticket.Payment != null ? new
                    {
                        ticket.Payment.PaymentId,
                        ticket.Payment.Amount,
                        ticket.Payment.PaymentMethod,
                        ticket.Payment.Status,
                        ticket.Payment.TransactionRef,
                        ticket.Payment.PaymentDate
                    } : null,
                    QRCode = qrCodeBase64 // Include QR code
                });
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

                if (ticket.Trip.DepartureTime <= DateTime.Now)
                {
                    return BadRequest(new { message = "Cannot cancel ticket after departure time" });
                }

                if (ticket.Status == "Cancelled")
                {
                    return BadRequest(new { message = "Ticket already cancelled" });
                }

                if (ticket.Status == "Used")
                {
                    return BadRequest(new { message = "Cannot cancel a used ticket" });
                }

                var oldStatus = ticket.Status;
                ticket.Status = "Cancelled";
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Ticket #{TicketId} cancelled by user. Status: {OldStatus} -> Cancelled (1 seat released)",
                    ticketId, oldStatus);

                return Ok(new { message = "Ticket cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling ticket {TicketId}", ticketId);
                return StatusCode(500, new { message = "Error cancelling ticket", error = ex.Message });
            }
        }

        // Helper method to generate QR Code
        // Helper method to generate QR Code - IMPROVED VERSION
        private string GenerateQRCode(int ticketId)
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    // ✅ TĂNG ERROR CORRECTION từ Q lên H (Cao nhất)
                    // H = High (30% có thể bị hư vẫn đọc được)
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(
                        ticketId.ToString(),
                        QRCodeGenerator.ECCLevel.H  // ✅ ĐỔI TỪ Q -> H
                    );

                    using (QRCode qrCode = new QRCode(qrCodeData))
                    {
                        // ✅ TĂNG KÍCH THƯỚC từ 20 lên 30 pixels per module
                        using (Bitmap qrCodeImage = qrCode.GetGraphic(
                            pixelsPerModule: 30,  // ✅ TĂNG TỪ 20 -> 30
                            darkColor: Color.Black,
                            lightColor: Color.White,
                            drawQuietZones: true  // ✅ THÊM QUIET ZONE (viền trắng xung quanh)
                        ))
                        {
                            // Convert to Base64 string
                            using (MemoryStream ms = new MemoryStream())
                            {
                                qrCodeImage.Save(ms, ImageFormat.Png);
                                byte[] byteImage = ms.ToArray();
                                return "data:image/png;base64," + Convert.ToBase64String(byteImage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for TicketId: {TicketId}", ticketId);
                return null;
            }
        }
    }
}