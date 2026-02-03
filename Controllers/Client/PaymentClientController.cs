//using Public_Transport.Models.EF;
//using Public_Transport.Models.Entities;
//using Public_Transport.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Cors.Infrastructure;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using Public_Transport.Models.EF;
//using Public_Transport.Services;
//using System.Linq;
//using System.Security.Claims;
//using System.Security.Cryptography;
//using System.Text;

//namespace Public_Transport.Controllers
//{
//    [Route("payment")]
//    [Authorize]
//    public class PaymentClientController : Controller
//    {
//        private readonly MoMoService _momoService;
//        private readonly IConfiguration _config;
//        private readonly ILogger<PaymentClientController> _logger;
//        private readonly ApplicationDbContext _context;
       
//        public PaymentClientController(
//            MoMoService momoService,
//            IConfiguration config,
//            ILogger<PaymentClientController> logger,
//            ApplicationDbContext context)
           
//        {
//            _momoService = momoService;
//            _config = config;
//            _logger = logger;
//            _context = context;
           
//        }

//        [HttpGet("momo")]
//        public async Task<IActionResult> PayWithMoMo([FromQuery] string addressId)
//        {
//            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
//            {
//                return RedirectToAction("Login", "Account");
//            }
//            //var cartItems = await _cartService.GetCartItemsAsync();
//            //if (cartItems == null || !cartItems.Any())
//            //{
//            //    return RedirectToAction("Index", "Cart");
//            //}

//            //long subtotal = await _cartService.GetSubtotalAsync();
//            long shippingFee = 25000;
//            long couponDiscount = 10000;
//            long finalTotal = subtotal + shippingFee - couponDiscount;

//            //var newOrder = new Order
//            //{
//            //    OrderDate = DateTime.Now,
//            //    Status = "Pending_Payment",
//            //    TotalAmount = (decimal)finalTotal,
//            //    PaymentMethod = "MoMo",
//            //    ShippingAddress = addressId,
//            //    UserUid = userId
//            //};

//            //foreach (var item in cartItems)
//            //{
//            //    _context.OrderItem.Add(new OrderItem
//            //    {
//            //        Order = newOrder,
//            //        ProductUid = item.ProductId,
//            //        Quantity = item.Quantity,
//            //        PriceAtPurchase = item.Price
//            //    });
//            //}

//            await _context.SaveChangesAsync();

//            string orderId = $"{newOrder.Uid}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

//            string orderInfo = "Payment for order #" + orderId;
//            long paymentAmount = finalTotal;

//            var payUrl = await _momoService.CreatePaymentAsync(paymentAmount, orderId, orderInfo);
//            _logger.LogInformation("Redirecting to MoMo PayUrl: {url}", payUrl);

//            return Redirect(payUrl);
//        }

//        [HttpGet("return")]
//        public async Task<IActionResult> PaymentReturn()
//        {
//            var query = Request.Query;
//            string resultCode = query["resultCode"].ToString();
//            string orderIdFromMoMo = query["orderId"].ToString();

//            Order order = null;

//            var mainOrderId = orderIdFromMoMo.Split('_')[0];
//            if (long.TryParse(mainOrderId, out long dbOrderId))
//            {
//                order = await _context.Order
//                    .Include(o => o.OrderItems)
//                    .ThenInclude(oi => oi.Product)
//                    .FirstOrDefaultAsync(o => o.Uid == dbOrderId);
//            }


//            if (resultCode == "0")
//            {
//                ViewBag.Result = "Payment successful!";
//                if (order != null)
//                {
//                    if (order.Status != "Paid")
//                    {
//                        order.Status = "Paid";

//                        _context.Order.Update(order);
//                        await _context.SaveChangesAsync();
//                    }
//                    await _cartService.ClearCartAsync();
//                }
//            }
//            else
//            {
//                ViewBag.Result = "Payment failed or cancelled.";
//                ViewBag.Message = query["message"];
//                if (order != null) order.Status = "Failed";
//            }

//            ViewBag.OrderId = query["orderId"];
//            ViewBag.Amount = query["amount"];

//            return View("Result", order);
//        }

//        [HttpPost("notify")]
//        [AllowAnonymous]
//        public async Task<IActionResult> PaymentNotify()
//        {
//            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
//            var body = await reader.ReadToEndAsync();

//            _logger.LogInformation("MoMo Notify Received: {body}", body);
//            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
//            if (data == null)
//            {
//                return BadRequest(new { message = "Invalid JSON" });
//            }

//            var secretKey = _config["MoMo:SecretKey"];
//            var accessKey = _config["MoMo:AccessKey"];

//            var rawHash =
//                $"accessKey={accessKey}" +
//                $"&amount={data["amount"]}" +
//                $"&extraData={data["extraData"]}" +
//                $"&message={data["message"]}" +
//                $"&orderId={data["orderId"]}" +
//                $"&orderInfo={data["orderInfo"]}" +
//                $"&orderType={data["orderType"]}" +
//                $"&partnerCode={data["partnerCode"]}" +
//                $"&payType={data["payType"]}" +
//                $"&requestId={data["requestId"]}" +
//                $"&responseTime={data["responseTime"]}" +
//                $"&resultCode={data["resultCode"]}" +
//                $"&transId={data["transId"]}";

//            var mySignature = CreateSignature(secretKey, rawHash);
//            var momoSig = data["signature"]?.ToString();

//            _logger.LogInformation("Data string to be hashed: {raw}", rawHash);
//            _logger.LogInformation("My Signature: {sig}", mySignature);
//            _logger.LogInformation("MoMo Signature: {sig}", momoSig);

//            if (mySignature == momoSig && data["resultCode"]?.ToString() == "0")
//            {
//                _logger.LogInformation("MoMo signature verified!");

//                string orderIdStr = data["orderId"]?.ToString();
//                var mainOrderId = orderIdStr?.Split('_')[0];
//                if (long.TryParse(mainOrderId, out long dbOrderId))
//                {
//                    var order = await _context.Order.FirstOrDefaultAsync(o => o.Uid == dbOrderId);

//                    if (order != null && order.Status == "Pending_Payment")
//                    {
//                        order.Status = "Paid";
//                        await _context.SaveChangesAsync();
//                        _logger.LogInformation("Order {OrderId} status updated to Paid.", dbOrderId);
//                    }
//                    else if (order != null)
//                    {
//                        _logger.LogWarning("Order {OrderId} already processed or status is not Pending. Status: {Status}", dbOrderId, order.Status);
//                    }
//                }

//                return Ok(new { message = "Payment verified successfully" });
//            }
//            else
//            {
//                _logger.LogWarning("Invalid signature or payment failed!");
//                return BadRequest(new { message = "Invalid signature" });
//            }
//        }

//        private static string CreateSignature(string key, string data)
//        {
//            var encoding = new UTF8Encoding();
//            byte[] keyByte = encoding.GetBytes(key);
//            byte[] messageBytes = encoding.GetBytes(data);
//            using var hmacsha256 = new HMACSHA256(keyByte);
//            byte[] hashMessage = hmacsha256.ComputeHash(messageBytes);
//            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
//        }
//    }
//}