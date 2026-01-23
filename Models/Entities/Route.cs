using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace Public_Transport.Models.Entities
{
    public class Route
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        [StringLength(100)]
        public string RouteName { get; set; }
        public string Description { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BasePrice { get; set; }
        public double TotalDistance { get; set; }
        public ICollection<RouteDetail> RouteDetails { get; set; }
        public ICollection<Trip> Trips { get; set; }
    }
}