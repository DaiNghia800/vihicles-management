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

        [ForeignKey("Vehicle")]
        public int? VehicleId { get; set; }

        public DateTime DepartureTime { get; set; } 
        public DateTime ArrivalTime { get; set; }

        [StringLength(20)]
        public string Status { get; set; } 


        public virtual Route? Route { get; set; }

        public virtual Vehicle? Vehicle { get; set; }

        public int? DriverId { get; set; }

        [ForeignKey("DriverId")]
        public virtual Driver? Driver { get; set; }
    }
}