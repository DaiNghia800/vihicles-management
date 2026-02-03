using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class Station
    {
        [Key]
        public int StationId { get; set; }

        [Required]
        [StringLength(100)]
        public string StationName { get; set; }

        [StringLength(255)]
        public string Address { get; set; }


        // [Column(TypeName = "varchar(50)")]
        // public string Coordinates { get; set; } 

        public double Latitude { get; set; }  
        public double Longitude { get; set; } 

        public virtual ICollection<RouteDetail>? RouteDetails { get; set; }
    }
}