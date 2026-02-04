namespace Public_Transport.Models.DTO
{
    public class PaymentDTO
    {
        public int PaymentId { get; set; }
        public int TicketId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string RouteName { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionRef { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime DepartureTime { get; set; }
    }
}