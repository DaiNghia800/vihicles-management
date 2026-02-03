using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Public_Transport.Controllers.Client
{
    [Route("payment")]
    [Authorize]
    public class PaymentClientController : Controller
    {
        private readonly MoMoService _momoService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentClientController> _logger;
        private readonly ApplicationDbContext _context;

        public PaymentClientController(
            MoMoService momoService,
            IConfiguration config,
            ILogger<PaymentClientController> logger,
            ApplicationDbContext context)
        {
            _momoService = momoService;
            _config = config;
            _logger = logger;
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                _logger.LogInformation("CreatePayment called with TripId: {TripId}", request.TripId);

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    _logger.LogWarning("User not authenticated");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                _logger.LogInformation("User authenticated: UserId = {UserId}", userId);

                // Kiểm tra Trip có tồn tại không
                var trip = await _context.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.TripId == request.TripId);

                if (trip == null)
                {
                    return NotFound(new { message = "Trip not found" });
                }

                // Tạo Ticket trước
                var ticket = new Ticket
                {
                    TripId = request.TripId,
                    UserId = userId,
                    Price = trip.Route.BasePrice,
                    Status = "Booked",
                    BookingDate = DateTime.Now
                };

                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync(); // ✅ Lưu ticket để có TicketId

                // ✅ TẠO ORDERID NGAY SAU KHI CÓ TICKETID
                string orderId = $"TICKET_{ticket.TicketId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                // ✅ Tạo Payment record với TransactionRef đầy đủ
                var payment = new Payment
                {
                    TicketId = ticket.TicketId,
                    Amount = trip.Route.BasePrice,
                    PaymentMethod = "Momo",
                    TransactionRef = orderId,  // ✅ Gán ngay từ đầu
                    Status = "Pending",
                    PaymentDate = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(); // ✅ Lưu payment với đầy đủ thông tin

                // Tạo link thanh toán MoMo
                string orderInfo = $"Payment for Trip {trip.Route.RouteName} - Ticket #{ticket.TicketId}";
                long paymentAmount = (long)trip.Route.BasePrice;

                var payUrl = await _momoService.CreatePaymentAsync(paymentAmount, orderId, orderInfo);

                _logger.LogInformation("Created payment for Ticket {TicketId}, PayUrl: {url}", ticket.TicketId, payUrl);

                return Ok(new
                {
                    success = true,
                    paymentUrl = payUrl,
                    ticketId = ticket.TicketId,
                    paymentId = payment.PaymentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for TripId: {TripId}", request.TripId);
                return StatusCode(500, new
                {
                    message = "Error creating payment",
                    error = ex.Message,
                    stackTrace = ex.StackTrace // Remove in production
                });
            }
        }

        [HttpGet("return")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentReturn()
        {
            var query = Request.Query;
            string resultCode = query["resultCode"].ToString();
            string orderIdFromMoMo = query["orderId"].ToString();

            Payment payment = null;

            // Tìm payment theo TransactionRef
            if (!string.IsNullOrEmpty(orderIdFromMoMo))
            {
                payment = await _context.Payments
                    .Include(p => p.Ticket)
                        .ThenInclude(t => t.Trip)
                            .ThenInclude(tr => tr.Route)
                    .Include(p => p.Ticket)
                        .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(p => p.TransactionRef == orderIdFromMoMo);
            }

            if (resultCode == "0")
            {
                ViewBag.Result = "Payment successful!";
                ViewBag.Success = true;

                if (payment != null && payment.Status != "Success")
                {
                    payment.Status = "Success";
                    payment.Ticket.Status = "Paid";

                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Payment {PaymentId} completed successfully", payment.PaymentId);
                }
            }
            else
            {
                ViewBag.Result = "Payment failed or cancelled.";
                ViewBag.Success = false;
                ViewBag.Message = query["message"];

                if (payment != null)
                {
                    payment.Status = "Failed";
                    payment.Ticket.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.OrderId = query["orderId"];
            ViewBag.Amount = query["amount"];

            return View("~/Views/Payment/Result.cshtml", payment);
        }

        [HttpPost("notify")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentNotify()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("MoMo Notify Received: {body}", body);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            if (data == null)
            {
                return BadRequest(new { message = "Invalid JSON" });
            }

            var secretKey = _config["MoMo:SecretKey"];
            var accessKey = _config["MoMo:AccessKey"];

            var rawHash =
                $"accessKey={accessKey}" +
                $"&amount={data["amount"]}" +
                $"&extraData={data["extraData"]}" +
                $"&message={data["message"]}" +
                $"&orderId={data["orderId"]}" +
                $"&orderInfo={data["orderInfo"]}" +
                $"&orderType={data["orderType"]}" +
                $"&partnerCode={data["partnerCode"]}" +
                $"&payType={data["payType"]}" +
                $"&requestId={data["requestId"]}" +
                $"&responseTime={data["responseTime"]}" +
                $"&resultCode={data["resultCode"]}" +
                $"&transId={data["transId"]}";

            var mySignature = CreateSignature(secretKey, rawHash);
            var momoSig = data["signature"]?.ToString();

            _logger.LogInformation("My Signature: {sig}", mySignature);
            _logger.LogInformation("MoMo Signature: {sig}", momoSig);

            if (mySignature == momoSig && data["resultCode"]?.ToString() == "0")
            {
                _logger.LogInformation("MoMo signature verified!");

                string orderIdStr = data["orderId"]?.ToString();
                var payment = await _context.Payments
                    .Include(p => p.Ticket)
                    .FirstOrDefaultAsync(p => p.TransactionRef == orderIdStr);

                if (payment != null && payment.Status == "Pending")
                {
                    payment.Status = "Success";
                    payment.Ticket.Status = "Paid";
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Payment {PaymentId} status updated to Success via IPN.", payment.PaymentId);
                }

                return Ok(new { message = "Payment verified successfully" });
            }
            else
            {
                _logger.LogWarning("Invalid signature or payment failed!");
                return BadRequest(new { message = "Invalid signature" });
            }
        }

        private static string CreateSignature(string key, string data)
        {
            var encoding = new UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);
            byte[] messageBytes = encoding.GetBytes(data);
            using var hmacsha256 = new HMACSHA256(keyByte);
            byte[] hashMessage = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
        }
        [HttpPost("retry/{ticketId}")]
        public async Task<IActionResult> RetryPayment(int ticketId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Lấy ticket và kiểm tra quyền sở hữu
                var ticket = await _context.Tickets
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Route)
                    .Include(t => t.Payment)
                    .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.UserId == userId);

                if (ticket == null)
                {
                    return NotFound(new { message = "Ticket not found" });
                }

                // Kiểm tra trạng thái ticket
                if (ticket.Status != "Booked")
                {
                    return BadRequest(new { message = "Only booked tickets can be paid" });
                }

                // Kiểm tra xem chuyến đi đã qua chưa
                if (ticket.Trip.DepartureTime <= DateTime.Now)
                {
                    return BadRequest(new { message = "Cannot pay for past trips" });
                }

                // Kiểm tra xem đã có payment chưa
                Payment payment;
                if (ticket.Payment != null)
                {
                    // Nếu đã có payment nhưng failed/pending, update nó
                    payment = ticket.Payment;
                    if (payment.Status == "Success")
                    {
                        return BadRequest(new { message = "This ticket has already been paid" });
                    }

                    // Update payment status về Pending
                    payment.Status = "Pending";
                    payment.PaymentDate = DateTime.Now;
                }
                else
                {
                    // Tạo payment mới
                    string orderId = $"TICKET_{ticket.TicketId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                    payment = new Payment
                    {
                        TicketId = ticket.TicketId,
                        Amount = ticket.Price,
                        PaymentMethod = "Momo",
                        TransactionRef = orderId,
                        Status = "Pending",
                        PaymentDate = DateTime.Now
                    };

                    _context.Payments.Add(payment);
                }

                await _context.SaveChangesAsync();

                // Tạo link thanh toán MoMo
                string orderInfo = $"Payment for Trip {ticket.Trip.Route.RouteName} - Ticket #{ticket.TicketId}";
                long paymentAmount = (long)ticket.Price;

                var payUrl = await _momoService.CreatePaymentAsync(paymentAmount, payment.TransactionRef, orderInfo);

                _logger.LogInformation("Retry payment for Ticket {TicketId}, PayUrl: {url}", ticket.TicketId, payUrl);

                return Ok(new
                {
                    success = true,
                    paymentUrl = payUrl,
                    ticketId = ticket.TicketId,
                    paymentId = payment.PaymentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying payment for TicketId: {TicketId}", ticketId);
                return StatusCode(500, new
                {
                    message = "Error processing payment",
                    error = ex.Message
                });
            }
        }
    }

    public class CreatePaymentRequest
    {
        public int TripId { get; set; }
    }
}