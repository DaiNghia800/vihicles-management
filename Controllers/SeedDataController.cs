using Microsoft.AspNetCore.Mvc;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Public_Transport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SeedDataController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("generate-sample")]
        public async Task<IActionResult> GenerateSampleData()
        {
            // 1. Kiểm tra xem đã có dữ liệu chưa, nếu có rồi thì thôi
            if (await _context.Routes.AnyAsync())
            {
                return BadRequest("Dữ liệu mẫu đã tồn tại rồi! Hãy xóa DB nếu muốn tạo lại.");
            }

            // 2. Tạo Trạm (Stations)
            var stations = new List<Station>
            {
                new Station { StationName = "Bến xe Miền Đông (Mới)", Address = "TP. Thủ Đức, HCM", Coordinates = "10.840, 106.800" }, // ID sẽ là 1
                new Station { StationName = "Ngã 4 Hàng Xanh", Address = "Bình Thạnh, HCM", Coordinates = "10.800, 106.700" },       // ID sẽ là 2
                new Station { StationName = "Cầu Sài Gòn", Address = "Bình Thạnh, HCM", Coordinates = "10.790, 106.710" },           // ID sẽ là 3
                new Station { StationName = "Ngã 4 Vũng Tàu", Address = "Biên Hòa, Đồng Nai", Coordinates = "10.900, 106.850" },     // ID sẽ là 4
                new Station { StationName = "Bến xe Vũng Tàu", Address = "Nam Kỳ Khởi Nghĩa, Vũng Tàu", Coordinates = "10.350, 107.080" } // ID sẽ là 5
            };
            await _context.Stations.AddRangeAsync(stations);
            await _context.SaveChangesAsync();

            // 3. Tạo Tuyến đường (Route) - Ví dụ Tuyến 15: BX Miền Đông -> Vũng Tàu
            var route = new Public_Transport.Models.Entities.Route
            {
                RouteName = "Tuyến 15: BX Miền Đông - Vũng Tàu",
                Description = "Xe chất lượng cao, chạy đường cao tốc Long Thành",
                BasePrice = 120000, // 120k
                TotalDistance = 95.5 // km
            };
            await _context.Routes.AddAsync(route);
            await _context.SaveChangesAsync();

            // 4. Tạo Chi tiết lộ trình (Nối Route với Station)
            // Logic: Lấy ID của Route vừa tạo và ID của các Station vừa tạo
            var routeDetails = new List<RouteDetail>
            {
                // Điểm đầu: BX Miền Đông
                new RouteDetail { RouteId = route.RouteId, StationId = stations[0].StationId, OrderIndex = 1, DistanceFromStart = 0, IsMajorStop = true },
                // Điểm 2: Hàng Xanh
                new RouteDetail { RouteId = route.RouteId, StationId = stations[1].StationId, OrderIndex = 2, DistanceFromStart = 5.2, IsMajorStop = false },
                // Điểm 3: Cầu Sài Gòn
                new RouteDetail { RouteId = route.RouteId, StationId = stations[2].StationId, OrderIndex = 3, DistanceFromStart = 7.0, IsMajorStop = false },
                // Điểm 4: Ngã 4 Vũng Tàu
                new RouteDetail { RouteId = route.RouteId, StationId = stations[3].StationId, OrderIndex = 4, DistanceFromStart = 30.5, IsMajorStop = true },
                // Điểm cuối: Vũng Tàu
                new RouteDetail { RouteId = route.RouteId, StationId = stations[4].StationId, OrderIndex = 5, DistanceFromStart = 95.5, IsMajorStop = true }
            };
            await _context.RouteDetails.AddRangeAsync(routeDetails);

            // 5. Tạo Xe (Vehicle)
            var vehicle = new Vehicle
            {
                LicensePlate = "59B-123.45",
                VehicleType = "Limousine 16 chỗ",
                SeatCapacity = 16,
                Status = "Active"
            };
            await _context.Vehicles.AddAsync(vehicle);
            await _context.SaveChangesAsync();

            // 6. Tạo Chuyến đi (Trip) - Feature chính của bạn
            var trips = new List<Trip>
            {
                // Chuyến 1: Chạy sáng nay
                new Trip
                {
                    RouteId = route.RouteId,
                    VehicleId = vehicle.VehicleId,
                    DepartureTime = DateTime.Now.AddHours(1), // Chạy sau 1 tiếng nữa
                    ArrivalTime = DateTime.Now.AddHours(3.5), // Chạy mất 2.5 tiếng
                    Status = "Scheduled"
                },
                // Chuyến 2: Chạy ngày mai
                new Trip
                {
                    RouteId = route.RouteId,
                    VehicleId = vehicle.VehicleId,
                    DepartureTime = DateTime.Now.AddDays(1).AddHours(8), // 8h sáng mai
                    ArrivalTime = DateTime.Now.AddDays(1).AddHours(10.5),
                    Status = "Scheduled"
                }
            };
            await _context.Trips.AddRangeAsync(trips);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã tạo dữ liệu mẫu thành công! Chiến thôi!" });
        }
    }
}
