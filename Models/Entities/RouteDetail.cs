using Public_Transport.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class RouteDetail
    {
        [Key]
        public int DetailId { get; set; }

        [ForeignKey("Route")]
        public int RouteId { get; set; }

        [ForeignKey("Station")]
        public int StationId { get; set; }

        public int OrderIndex { get; set; } // Thứ tự: 1, 2, 3...
        public double DistanceFromStart { get; set; }
        public bool IsMajorStop { get; set; } // Trạm chính hay phụ

        // Relationship
        public virtual Route Route { get; set; }
        public virtual Station Station { get; set; }
    }
}