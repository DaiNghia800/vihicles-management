using Microsoft.AspNetCore.Mvc;
using Public_Transport.Models.EF;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/dashboard")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public IActionResult Dashboard()
        {
            return View("~/Views/Admin/Dashboard.cshtml");
        }
    }
}
