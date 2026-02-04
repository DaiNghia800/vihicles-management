using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.DTO;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. EXISTING METHODS (CODE CŨ CỦA EM) ---

        public int getVehicleActive()
        {
            try
            {
                return _context.Vehicles.Count(p => p.Status == "Active" && !p.Deleted);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public int getDailyPassengers()
        {
            try
            {
                return _context.Tickets.Count(p => p.Status == "Used" && p.BookingDate.Date == DateTime.Today && p.BookingDate.Hour <= DateTime.Now.Hour);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public int getOperatingTripsToday()
        {
            try
            {
                var now = DateTime.Now;
                var today = DateTime.Today;

                return _context.Trips.Count(t =>
                    (t.Status == "Running" || t.Status == "Completed") &&
                    t.DepartureTime >= today &&
                    t.DepartureTime <= now
                );
            }
            catch
            {
                return -1;
            }
        }

        public List<TrafficFlowDTO> GetTrafficFlow()
        {
            try
            {
                var now = DateTime.Now;
                var fromTime = now.AddHours(-24);

                // Lấy dữ liệu raw từ DB
                var rawData = _context.Tickets
                    .Where(t =>
                        t.Status == "Used" &&
                        t.BookingDate >= fromTime &&
                        t.BookingDate <= now
                    )
                    .GroupBy(t => t.BookingDate.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        Count = g.Count()
                    })
                    .ToList();

                // Chuẩn hóa đủ 24 giờ
                var result = new List<TrafficFlowDTO>();

                for (int i = 0; i < 24; i++)
                {
                    var item = rawData.FirstOrDefault(x => x.Hour == i);

                    result.Add(new TrafficFlowDTO
                    {
                        Hour = i,
                        HourLabel = $"{i:00}:00",
                        PassengerCount = item?.Count ?? 0
                    });
                }

                return result;
            }
            catch
            {
                return new List<TrafficFlowDTO>();
            }
        }

        // --- 2. NEW METHODS (CODE MỚI ANH THÊM CHO DASHBOARD) ---

        // Lấy 5 chuyến xe gần nhất & tính toán trạng thái Delay
        public List<object> GetRecentTrips()
        {
            try
            {
                var trips = _context.Trips
                    .Include(t => t.Route)
                    .OrderByDescending(t => t.DepartureTime) // Lấy chuyến mới nhất
                    .Take(5)
                    .ToList() // Tải về bộ nhớ trước để xử lý logic ngày tháng
                    .Select(t => new
                    {
                        RouteName = t.Route?.RouteName ?? "Unknown Route",
                        Status = t.Status,
                        // Logic Delay: Nếu đang chạy (Running) mà giờ hiện tại > giờ đến dự kiến => Trễ
                        Delay = (t.Status == "Running" && DateTime.Now > t.ArrivalTime) ?
                                (int)(DateTime.Now - t.ArrivalTime).TotalMinutes : 0,
                        IsDelayed = (t.Status == "Running" && DateTime.Now > t.ArrivalTime)
                    })
                    .ToList<object>();

                return trips;
            }
            catch
            {
                return new List<object>();
            }
        }

        // Lấy dữ liệu vẽ bản đồ (Routes + Stations)
        public object GetMapData()
        {
            try
            {
                var routes = _context.Routes
                    .Include(r => r.RouteDetails)
                    .ThenInclude(rd => rd.Station)
                    .Where(r => r.RouteDetails.Any()) // Chỉ lấy tuyến có trạm
                    .Select(r => new
                    {
                        RouteName = r.RouteName,
                        Stations = r.RouteDetails.OrderBy(rd => rd.OrderIndex).Select(rd => new
                        {
                            Name = rd.Station.StationName,
                            Lat = rd.Station.Latitude,
                            Lng = rd.Station.Longitude
                        }).ToList()
                    })
                    .ToList();
                return routes;
            }
            catch
            {
                return new List<object>();
            }
        }

        // Đếm số lượng chuyến bị hủy (Incident/Alerts)
        public int GetIncidentCount()
        {
            try
            {
                return _context.Trips.Count(t => t.Status == "Cancelled");
            }
            catch
            {
                return 0;
            }
        }
    }
}