using System.Security.Claims;
using Public_Transport.Helpers;
using Public_Transport.Helpers;

namespace Public_Transport.Middleware
{
    public class AdminAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // CHỈ KIỂM TRA NẾU PATH BẮT ĐẦU BẰNG /admin
            if (path != null && path.StartsWith("/admin"))
            {
                // Kiểm tra user đã đăng nhập chưa
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

                    // Nếu là Customer thì redirect đến Access Denied
                    if (userRole == WebConstants.ROLE_CUSTOMER)
                    {
                        context.Response.Redirect("/access-denied");
                        return;
                    }
                }
                else
                {
                    // Nếu chưa đăng nhập thì redirect về login
                    context.Response.Redirect("/login");
                    return;
                }
            }

            // Cho phép request tiếp tục
            await _next(context);
        }
    }
}