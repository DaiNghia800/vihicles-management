using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using Public_Transport.Models.DTO;

namespace Public_Transport.Controllers.Admin
{
    [Route("admin/payment")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ApplicationDbContext context, ILogger<PaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // View chính: Danh sách thanh toán
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/Payment/Index.cshtml");
        }

        // API: Lấy danh sách thanh toán với PAGINATION
        [HttpGet("api/list")]
        public async Task<IActionResult> GetPayments(
            [FromQuery] string status = null,
            [FromQuery] string paymentMethod = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Payments
                    .Include(p => p.Ticket)
                        .ThenInclude(t => t.User)
                    .Include(p => p.Ticket)
                        .ThenInclude(t => t.Trip)
                            .ThenInclude(tr => tr.Route)
                    .AsQueryable();

                // Lọc theo status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                // Lọc theo phương thức thanh toán
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query = query.Where(p => p.PaymentMethod == paymentMethod);
                }

                // Lọc theo khoảng thời gian
                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate <= toDate.Value);
                }

                // ✅ Đếm tổng số payments
                var totalItems = await query.CountAsync();

                // ✅ Lấy payments theo trang
                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        TicketId = p.TicketId,
                        CustomerName = p.Ticket.User.FullName,
                        CustomerEmail = p.Ticket.User.Email,
                        RouteName = p.Ticket.Trip.Route.RouteName,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        TransactionRef = p.TransactionRef,
                        Status = p.Status,
                        PaymentDate = p.PaymentDate,
                        DepartureTime = p.Ticket.Trip.DepartureTime
                    })
                    .ToListAsync();

                // ✅ Trả về kèm pagination info
                return Ok(new
                {
                    payments,
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
                return StatusCode(500, new { message = "Error loading payment list", error = ex.Message });
            }
        }

        // API: Lấy chi tiết thanh toán
        [HttpGet("api/detail/{id}")]
        public async Task<IActionResult> GetPaymentDetail(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Ticket)
                        .ThenInclude(t => t.User)
                    .Include(p => p.Ticket)
                        .ThenInclude(t => t.Trip)
                            .ThenInclude(tr => tr.Route)
                    .Include(p => p.Ticket)
                        .ThenInclude(t => t.Trip)
                            .ThenInclude(tr => tr.Vehicle)
                    .Where(p => p.PaymentId == id)
                    .Select(p => new
                    {
                        p.PaymentId,
                        p.TicketId,
                        p.Amount,
                        p.PaymentMethod,
                        p.TransactionRef,
                        p.Status,
                        p.PaymentDate,
                        Ticket = new
                        {
                            p.Ticket.TicketId,
                            p.Ticket.Price,
                            p.Ticket.Status,
                            p.Ticket.BookingDate
                        },
                        Customer = new
                        {
                            p.Ticket.User.Uid,
                            p.Ticket.User.FullName,
                            p.Ticket.User.Email,
                            p.Ticket.User.PhoneNumber
                        },
                        Trip = new
                        {
                            p.Ticket.Trip.TripId,
                            Route = p.Ticket.Trip.Route.RouteName,
                            p.Ticket.Trip.DepartureTime,
                            p.Ticket.Trip.ArrivalTime,
                            Vehicle = p.Ticket.Trip.Vehicle.LicensePlate,
                            VehicleType = p.Ticket.Trip.Vehicle.VehicleType
                        }
                    })
                    .FirstOrDefaultAsync();

                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading payment detail", error = ex.Message });
            }
        }

        // API: Cập nhật trạng thái thanh toán
        [HttpPut("api/update-status/{id}")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                payment.Status = request.Status;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating status", error = ex.Message });
            }
        }

        // API: Đánh dấu vé đã sử dụng
        [HttpPut("api/mark-ticket-used/{ticketId}")]
        public async Task<IActionResult> MarkTicketAsUsed(int ticketId)
        {
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Trip)
                    .FirstOrDefaultAsync(t => t.TicketId == ticketId);

                if (ticket == null)
                {
                    return NotFound(new { message = "Ticket not found" });
                }

                // Kiểm tra trạng thái hiện tại
                if (ticket.Status == "Used")
                {
                    return BadRequest(new { message = "This ticket has already been used" });
                }

                if (ticket.Status == "Cancelled")
                {
                    return BadRequest(new { message = "Cannot mark a cancelled ticket as used" });
                }

                if (ticket.Status != "Paid")
                {
                    return BadRequest(new { message = "Only paid tickets can be marked as used" });
                }

                // ✅ Loại bỏ kiểm tra thời gian
                if (ticket.Trip.DepartureTime > DateTime.Now)
                {
                    var departureTimeStr = ticket.Trip.DepartureTime.ToString("dd/MM/yyyy HH:mm");
                    return BadRequest(new { 
                        message = $"Cannot mark as used. Trip departs at {departureTimeStr}. Please wait until departure time.",
                        departureTime = departureTimeStr
                    });
                }

                // ✅ Paid -> Used (vẫn giữ slot +1, chỉ đổi trạng thái)
                var oldStatus = ticket.Status;
                ticket.Status = "Used";
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Ticket #{TicketId} marked as used. Status: {OldStatus} -> Used (slot maintained)", 
                    ticketId, oldStatus);

                return Ok(new { message = "Ticket marked as used successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket status for TicketId: {TicketId}", ticketId);
                return StatusCode(500, new { message = "Error updating ticket status", error = ex.Message });
            }
        }

        // API: Lấy danh sách tickets với filter VÀ PAGINATION
        [HttpGet("api/tickets")]
        public async Task<IActionResult> GetTickets(
            [FromQuery] string status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Tickets
                    .Include(t => t.User)
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Route)
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Vehicle)
                    .Include(t => t.Payment)
                    .AsQueryable();

                // Lọc theo status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(t => t.Status == status);
                }

                // Lọc theo khoảng thời gian
                if (fromDate.HasValue)
                {
                    query = query.Where(t => t.BookingDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(t => t.BookingDate <= toDate.Value);
                }

                // Tìm kiếm theo tên khách hàng hoặc email
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(t =>
                        t.User.FullName.ToLower().Contains(searchTerm) ||
                        t.User.Email.ToLower().Contains(searchTerm) ||
                        t.TicketId.ToString().Contains(searchTerm));
                }

                // Đếm tổng số tickets
                var totalItems = await query.CountAsync();

                // Lấy tickets theo trang
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
                        Customer = new
                        {
                            t.User.FullName,
                            t.User.Email,
                            t.User.PhoneNumber
                        },
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
                            t.Payment.PaymentMethod,
                            t.Payment.Status,
                            t.Payment.PaymentDate
                        } : null
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
                return StatusCode(500, new { message = "Error loading ticket list", error = ex.Message });
            }
        }

        // API: Scan QR Code và đánh dấu vé đã sử dụng
        [HttpPost("api/scan-ticket/{ticketId}")]
        public async Task<IActionResult> ScanTicket(int ticketId)
        {
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.User)
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Route)
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Vehicle)
                    .FirstOrDefaultAsync(t => t.TicketId == ticketId);

                if (ticket == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Ticket does not exist"
                    });
                }

                // Kiểm tra trạng thái
                if (ticket.Status == "Used")
                {
                    return Ok(new
                    {
                        success = false,
                        message = "This ticket has already been used",
                        ticket = new
                        {
                            ticket.TicketId,
                            ticket.Status,
                            CustomerName = ticket.User.FullName,
                            RouteName = ticket.Trip.Route.RouteName
                        }
                    });
                }

                if (ticket.Status == "Cancelled")
                {
                    return Ok(new
                    {
                        success = false,
                        message = "This ticket has been cancelled",
                        ticket = new
                        {
                            ticket.TicketId,
                            ticket.Status,
                            CustomerName = ticket.User.FullName
                        }
                    });
                }

                if (ticket.Status != "Paid")
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Ticket has not been paid",
                        ticket = new
                        {
                            ticket.TicketId,
                            ticket.Status,
                            CustomerName = ticket.User.FullName
                        }
                    });
                }

                // Đánh dấu vé đã sử dụng
                ticket.Status = "Used";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Ticket verified successfully",
                    ticket = new
                    {
                        ticket.TicketId,
                        ticket.Status,
                        CustomerName = ticket.User.FullName,
                        CustomerEmail = ticket.User.Email,
                        RouteName = ticket.Trip.Route.RouteName,
                        DepartureTime = ticket.Trip.DepartureTime,
                        VehicleType = ticket.Trip.Vehicle?.VehicleType,
                        LicensePlate = ticket.Trip.Vehicle?.LicensePlate,
                        Price = ticket.Price
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing ticket",
                    error = ex.Message
                });
            }
        }

        // API: Thống kê doanh thu
        [HttpGet("api/statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.Payments.AsQueryable();

                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate <= toDate.Value);
                }

                var statistics = new
                {
                    TotalRevenue = await query.Where(p => p.Status == "Success").SumAsync(p => p.Amount),
                    TotalTransactions = await query.CountAsync(),
                    SuccessfulTransactions = await query.Where(p => p.Status == "Success").CountAsync(),
                    PendingTransactions = await query.Where(p => p.Status == "Pending").CountAsync(),
                    FailedTransactions = await query.Where(p => p.Status == "Failed").CountAsync(),
                    PaymentMethods = await query
                        .Where(p => p.Status == "Success")
                        .GroupBy(p => p.PaymentMethod)
                        .Select(g => new { Method = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                        .ToListAsync()
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading statistics", error = ex.Message });
            }
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; }
    }
}