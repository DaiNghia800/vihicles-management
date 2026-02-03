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

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // View chính: Danh sách thanh toán
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/Payment/Index.cshtml");
        }

        // API: Lấy danh sách thanh toán
        [HttpGet("api/list")]
        public async Task<IActionResult> GetPayments(
            [FromQuery] string status = null,
            [FromQuery] string paymentMethod = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
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

                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
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

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách thanh toán", error = ex.Message });
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
                    return NotFound(new { message = "Không tìm thấy thanh toán" });
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải chi tiết thanh toán", error = ex.Message });
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
                    return NotFound(new { message = "Không tìm thấy thanh toán" });
                }

                payment.Status = request.Status;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái", error = ex.Message });
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
                return StatusCode(500, new { message = "Lỗi khi tải thống kê", error = ex.Message });
            }
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; }
    }
}