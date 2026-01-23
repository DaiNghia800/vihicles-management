using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.Entities 
{
    public class Station
    {
        [Key]
        public int StationId { get; set; }

        [Required]
        [StringLength(100)]
        public string StationName { get; set; } 

        public string Address { get; set; }
        public string Coordinates { get; set; } 

        // Relationship
        public ICollection<RouteDetail> RouteDetails { get; set; }
    }
}