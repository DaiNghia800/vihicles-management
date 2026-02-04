using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services;
using Public_Transport.Extensions; // ✅ THÊM DÒNG NÀY
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

                // ✅ KIỂM TRA SỐ CHỖ TRỐNG TRƯỚC KHI TẠO TICKET
                var (canBook, message) = await _context.CanBookTicketAsync(request.TripId);
                if (!canBook)
                {
                    _logger.LogWarning("Cannot book ticket for TripId {TripId}: {Message}", request.TripId, message);
                    return BadRequest(new { 
                        success = false, 
                        message = message 
                    });
                }

                // Kiểm tra Trip có tồn tại không
                var trip = await _context.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.TripId == request.TripId);

                if (trip == null)
                {
                    return NotFound(new { message = "Trip not found" });
                }

                // ✅ Tạo Ticket với status "Booked" (+1 slot)
                var ticket = new Ticket
                {
                    TripId = request.TripId,
                    UserId = userId,
                    Price = trip.Route.BasePrice,
                    Status = "Booked", // ✅ Ticket mới = Booked (+1 slot)
                    BookingDate = DateTime.Now
                };

                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Ticket #{TicketId} created with status 'Booked' (+1 seat occupied)", ticket.TicketId);

                // TẠO ORDERID
                string orderId = $"TICKET_{ticket.TicketId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                // Tạo Payment record
                var payment = new Payment
                {
                    TicketId = ticket.TicketId,
                    Amount = trip.Route.BasePrice,
                    PaymentMethod = "Momo",
                    TransactionRef = orderId,
                    Status = "Pending",
                    PaymentDate = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Tạo link thanh toán MoMo
                string orderInfo = $"Payment for Trip {trip.Route.RouteName} - Ticket #{ticket.TicketId}";
                long paymentAmount = (long)trip.Route.BasePrice;

                var payUrl = await _momoService.CreatePaymentAsync(paymentAmount, orderId, orderInfo);

                _logger.LogInformation("Created payment for Ticket {TicketId}, PayUrl: {url}", ticket.TicketId, payUrl);

                // ✅ Lấy thông tin số chỗ còn lại
                var remainingSeats = await _context.GetAvailableSeatsAsync(request.TripId);

                return Ok(new
                {
                    success = true,
                    paymentUrl = payUrl,
                    ticketId = ticket.TicketId,
                    paymentId = payment.PaymentId,
                    seatsInfo = new
                    {
                        totalCapacity = trip.Vehicle.SeatCapacity,
                        remainingSeats = remainingSeats,
                        bookedSeats = trip.Vehicle.SeatCapacity - remainingSeats
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for TripId: {TripId}", request.TripId);
                return StatusCode(500, new
                {
                    message = "Error creating payment",
                    error = ex.Message
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
                    var oldStatus = payment.Ticket.Status;
                    
                    payment.Status = "Success";
                    payment.Ticket.Status = "Paid"; // ✅ Booked -> Paid (vẫn giữ +1 slot)

                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("✅ Ticket #{TicketId} status changed: {OldStatus} -> Paid (slot maintained at +1)", 
                        payment.Ticket.TicketId, oldStatus);
                }
            }
            else
            {
                ViewBag.Result = "Payment failed or cancelled.";
                ViewBag.Success = false;
                ViewBag.Message = query["message"];

                if (payment != null)
                {
                    var oldStatus = payment.Ticket.Status;
                    
                    payment.Status = "Failed";
                    payment.Ticket.Status = "Cancelled"; // ✅ Booked -> Cancelled (-1 slot released)
                    
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("❌ Ticket #{TicketId} status changed: {OldStatus} -> Cancelled (1 seat released)", 
                        payment.Ticket.TicketId, oldStatus);
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
                    _logger.LogWarning("User not authenticated for retry payment");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "You need to log in to continue"
                    });
                }

                _logger.LogInformation("RetryPayment called for TicketId: {TicketId} by UserId: {UserId}", ticketId, userId);

                // Lấy ticket và kiểm tra quyền sở hữu
                var ticket = await _context.Tickets
                    .Include(t => t.Trip)
                        .ThenInclude(tr => tr.Route)
                    .Include(t => t.Payment)
                    .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.UserId == userId);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket not found or access denied: TicketId={TicketId}, UserId={UserId}", ticketId, userId);
                    return NotFound(new
                    {
                        success = false,
                        message = "Ticket not found or you don't have permission to access it"
                    });
                }

                // ✅ KIỂM TRA STATUS
                if (ticket.Status != "Booked")
                {
                    string statusMessage = ticket.Status switch
                    {
                        "Paid" => "This ticket has already been paid",
                        "Cancelled" => "This ticket has been cancelled and cannot be paid",
                        "Used" => "This ticket has already been used",
                        _ => $"Cannot process payment for ticket with status: {ticket.Status}"
                    };

                    _logger.LogWarning("Invalid ticket status for payment: TicketId={TicketId}, Status={Status}", ticketId, ticket.Status);
                    return BadRequest(new
                    {
                        success = false,
                        message = statusMessage
                    });
                }

                // ✅ GRACE PERIOD: 30 phút sau departure time
                var gracePeriodMinutes = 30; // ĐỔI TỪ var gracePeriodHours = 2;
                var paymentDeadline = ticket.Trip.DepartureTime.AddMinutes(gracePeriodMinutes);
                var now = DateTime.Now;

                if (now > paymentDeadline)
                {
                    var departureTimeStr = ticket.Trip.DepartureTime.ToString("dd/MM/yyyy HH:mm");
                    var deadlineTimeStr = paymentDeadline.ToString("dd/MM/yyyy HH:mm");

                    _logger.LogWarning("Payment deadline expired: TicketId={TicketId}, Deadline={Deadline}, Now={Now}",
                        ticketId, paymentDeadline, now);

                    return BadRequest(new
                    {
                        success = false,
                        message = $"Payment time has expired. This trip departed at {departureTimeStr}. Payment deadline was {deadlineTimeStr}.",
                        departureTime = departureTimeStr,
                        deadline = deadlineTimeStr
                    });
                }

                // ✅ KIỂM TRA XEM CÒN BAO NHIÊU THỜI GIAN
                var remainingTime = paymentDeadline - now;
                var isUrgent = remainingTime.TotalMinutes <= 10; // Cảnh báo nếu còn dưới 10 phút

                // Kiểm tra xem đã có payment chưa
                Payment payment;
                string orderId;

                if (ticket.Payment != null)
                {
                    payment = ticket.Payment;

                    if (payment.Status == "Success")
                    {
                        _logger.LogWarning("Ticket already paid: TicketId={TicketId}", ticketId);
                        return BadRequest(new
                        {
                            success = false,
                            message = "This ticket has already been paid successfully"
                        });
                    }

                    // Tạo orderId mới cho lần retry
                    orderId = $"TICKET_{ticket.TicketId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                    payment.TransactionRef = orderId;
                    payment.Status = "Pending";
                    payment.PaymentDate = DateTime.Now;

                    _logger.LogInformation("Updating existing payment: PaymentId={PaymentId}, NewTransactionRef={TransactionRef}",
                        payment.PaymentId, orderId);
                }
                else
                {
                    // Tạo payment mới
                    orderId = $"TICKET_{ticket.TicketId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

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
                    _logger.LogInformation("Creating new payment: TicketId={TicketId}, TransactionRef={TransactionRef}",
                        ticketId, orderId);
                }

                await _context.SaveChangesAsync();

                // Tạo link thanh toán MoMo
                string orderInfo = $"Payment for Trip {ticket.Trip.Route.RouteName} - Ticket #{ticket.TicketId}";
                long paymentAmount = (long)ticket.Price;

                _logger.LogInformation("Creating MoMo payment link: Amount={Amount}, OrderId={OrderId}", paymentAmount, orderId);

                string payUrl;
                try
                {
                    payUrl = await _momoService.CreatePaymentAsync(paymentAmount, orderId, orderInfo);
                    _logger.LogInformation("MoMo payment link created successfully: {PayUrl}", payUrl);
                }
                catch (Exception momoEx)
                {
                    _logger.LogError(momoEx, "MoMo service error for TicketId: {TicketId}", ticketId);
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Unable to connect to payment gateway. Please try again later.",
                        technicalError = momoEx.Message
                    });
                }

                // ✅ TRẢ VỀ VỚI WARNING NẾU GẦN HẾT HẠN
                return Ok(new
                {
                    success = true,
                    paymentUrl = payUrl,
                    ticketId = ticket.TicketId,
                    paymentId = payment.PaymentId,
                    // ✅ Thông tin thời gian còn lại
                    timeInfo = new
                    {
                        departureTime = ticket.Trip.DepartureTime.ToString("dd/MM/yyyy HH:mm"),
                        paymentDeadline = paymentDeadline.ToString("dd/MM/yyyy HH:mm"),
                        remainingMinutes = (int)remainingTime.TotalMinutes,
                        isUrgent = isUrgent,
                        warningMessage = isUrgent
                            ? $"⚠️ Warning: Only {(int)remainingTime.TotalMinutes} minutes left to complete payment!"
                            : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in RetryPayment for TicketId: {TicketId}", ticketId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred. Please contact support if this persists.",
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