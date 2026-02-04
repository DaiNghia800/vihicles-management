using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.Entities
{
    public class Vehicle
    {
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "The license plate must not be left blank.")]
        [StringLength(15, ErrorMessage = "Vehicle license plates can have a maximum of 15 characters.")]
        public string LicensePlate { get; set; } 
        public string Thumbnail { get; set; }

        [Required(ErrorMessage = "The vehicle type cannot be left blank.")]
        [StringLength(50)]
        public string VehicleType { get; set; }

        [Range(1, 100, ErrorMessage = "The number of seats must be greater than 0.")]
        public int SeatCapacity { get; set; }   
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool Deleted { get; set; }

        //public ICollection<Trip> Trips { get; set; }
    }
}
