using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [ForeignKey("Trip")]
        public int TripId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; } // Giá vé tại thời điểm mua

        [StringLength(50)]
        public string Status { get; set; } // Booked, Cancelled, Used

        public DateTime BookingDate { get; set; }

        // Relationships
        public virtual Trip Trip { get; set; }
        public virtual Users User { get; set; }
        public virtual Payment Payment { get; set; }
    }
}