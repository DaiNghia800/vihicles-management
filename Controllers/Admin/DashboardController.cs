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
            int totalDailyPassengers = _dashboardService.getDailyPassengers();
            int totalOperatingRoutes = _dashboardService.getOperatingTripsToday();
            ViewData["totalVehicleActive"] = totalVehicleActive;
            ViewData["totalDailyPassengers"] = totalDailyPassengers;
            ViewData["totalOperatingRoutes"] = totalOperatingRoutes;
            return View("~/Views/Admin/Dashboard.cshtml");
        }

        [HttpGet("traffic-flow")]
        public IActionResult GetTrafficFlow()
        {
            return Ok(_dashboardService.GetTrafficFlow());
        }
    }
}
