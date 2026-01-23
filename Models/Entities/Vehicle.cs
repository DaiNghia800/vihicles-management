using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.Entities
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }

        [Required]
        public string LicensePlate { get; set; } // Biển số xe: 59-X1 123.45

        public string VehicleType { get; set; } // Giường nằm, Ghế ngồi
        public int SeatCapacity { get; set; }   // Số chỗ ngồi
        public string Status { get; set; }      // Active, Maintenance

        public ICollection<Trip> Trips { get; set; }
    }
}