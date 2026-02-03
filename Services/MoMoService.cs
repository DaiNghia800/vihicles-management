using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Public_Transport.Services
{
    public class MoMoService
    {
        private readonly IConfiguration _config;

        public MoMoService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> CreatePaymentAsync(long amount, string orderId, string orderInfo)
        {
            var endpoint = _config["MoMo:Endpoint"];
            var partnerCode = _config["MoMo:PartnerCode"];
            var accessKey = _config["MoMo:AccessKey"];
            var secretKey = _config["MoMo:SecretKey"];

            var baseUrl = _config["PaymentSettings:BaseUrl"];
            var returnPath = _config["PaymentSettings:ReturnPath"];
            var notifyPath = _config["PaymentSettings:NotifyPath"];

            var requestId = Guid.NewGuid().ToString();

            // Ghép chuỗi tạo URL hoàn chỉnh
            var redirectUrl = $"{baseUrl}{returnPath}";
            var ipnUrl = $"{baseUrl}{notifyPath}";
            var requestType = "captureWallet";

            // Tạo raw signature
            var rawHash =
                $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";

            var signature = CreateSignature(secretKey, rawHash);

            var message = new
            {
                partnerCode,
                accessKey,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                requestType,
                extraData = "",
                signature,
                lang = "en"
            };

            // ⚙️ Tạo HttpClient có bỏ qua kiểm tra SSL (chỉ dùng khi test)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            var content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

            // Nếu lỗi từ MoMo (không có payUrl)
            if (jsonResponse == null || jsonResponse.payUrl == null)
            {
                throw new Exception("Không nhận được phản hồi hợp lệ từ MoMo: " + responseContent);
            }

            return jsonResponse.payUrl.ToString(); // Link thanh toán
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
    }
}
