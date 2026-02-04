using Microsoft.AspNetCore.Mvc;
using Public_Transport.Models.EF;
using Public_Transport.Services.IServices;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/dashboard")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("")]
        public IActionResult Dashboard()
        {
            int totalVehicleActive = _dashboardService.getVehicleActive();
            ViewData["totalVehicleActive"] = totalVehicleActive;
            return View("~/Views/Admin/Dashboard.cshtml");
        }
    }
}
