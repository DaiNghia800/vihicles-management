using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Public_Transport.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [ForeignKey("Ticket")]
        public int TicketId { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; } // Số tiền

        [Required]
        [StringLength(100)]
        public string PaymentMethod { get; set; } // Credit Card, Wallet, Cash, Momo, VNPAY

        [StringLength(200)]
        public string TransactionRef { get; set; } // Mã giao dịch từ cổng thanh toán

        [StringLength(50)]
        public string Status { get; set; } // Pending, Success, Failed

        public DateTime PaymentDate { get; set; } // Thời gian thanh toán

        // Relationship
        public virtual Ticket Ticket { get; set; }
    }
}