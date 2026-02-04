using Microsoft.AspNetCore.Mvc;
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
            // 1. Lấy các chỉ số thống kê cơ bản
            int totalVehicleActive = _dashboardService.getVehicleActive();
            int totalDailyPassengers = _dashboardService.getDailyPassengers();
            int totalOperatingRoutes = _dashboardService.getOperatingTripsToday();

            // 2. Lấy chỉ số Sự cố (Incidents) mới thêm
            int totalIncidents = _dashboardService.GetIncidentCount();

            // 3. Đẩy ra View
            ViewData["totalVehicleActive"] = totalVehicleActive;
            ViewData["totalDailyPassengers"] = totalDailyPassengers;
            ViewData["totalOperatingRoutes"] = totalOperatingRoutes;
            ViewData["totalIncidents"] = totalIncidents; // Dữ liệu mới cho ô màu đỏ

            return View("~/Views/Admin/Dashboard.cshtml");
        }

        // API lấy dữ liệu biểu đồ lưu lượng (Code cũ của bạn em)
        [HttpGet("traffic-flow")]
        public IActionResult GetTrafficFlow()
        {
            return Ok(_dashboardService.GetTrafficFlow());
        }

        // --- API MỚI 1: Lấy danh sách 5 chuyến xe gần nhất ---
        // Gọi bởi AJAX trong Dashboard.cshtml để đổ vào bảng Recent Trip Status
        [HttpGet("recent-trips")]
        public IActionResult GetRecentTrips()
        {
            return Ok(_dashboardService.GetRecentTrips());
        }

        // --- API MỚI 2: Lấy dữ liệu vẽ bản đồ ---
        // Gọi bởi AJAX trong Dashboard.cshtml để vẽ đường đi trên bản đồ Leaflet
        [HttpGet("map-data")]
        public IActionResult GetMapData()
        {
            return Ok(_dashboardService.GetMapData());
        }
    }
}