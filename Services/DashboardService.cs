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
            catch (Exception ex) {
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

    }
}
