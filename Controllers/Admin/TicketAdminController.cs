using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Public_Transport.Controllers.Admin
{
    [Route("admin/tickets")]
    [Authorize] // Thêm authorization middleware để chỉ admin mới truy cập được
    public class TicketAdminController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/Ticket/Index.cshtml");
        }
    }
}