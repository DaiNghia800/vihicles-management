using Public_Transport.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class Trip
    {
        [Key]
        public int TripId { get; set; }

        [ForeignKey("Route")]
        public int RouteId { get; set; }

        // DriverId liên kết với bảng Users (Role Driver) hoặc bảng Drivers riêng tùy bạn
        // Ở đây mình tạm để User (nếu bạn chưa tách bảng Driver riêng)
        public int? DriverId { get; set; }

        [ForeignKey("Vehicle")]
        public int? VehicleId { get; set; }

        public DateTime DepartureTime { get; set; } // Giờ chạy thực tế
        public DateTime ArrivalTime { get; set; }
        public string Status { get; set; } // Scheduled, Running, Completed

        // Relationship
        public virtual Route Route { get; set; }
        public virtual Vehicle Vehicle { get; set; }
        // public virtual User Driver { get; set; } // Uncomment nếu đã có bảng User
    }
}