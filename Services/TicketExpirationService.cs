using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;

namespace Public_Transport.Services
{
    public class TicketExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TicketExpirationService> _logger;
        private readonly int _bookingExpiryMinutes = 10; // ✅ 10 PHÚT SAU KHI BOOKING
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // ✅ Check mỗi 1 phút

        public TicketExpirationService(
            IServiceScopeFactory scopeFactory,
            ILogger<TicketExpirationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ Ticket Expiration Service started (Booking Expiry: {Minutes} minutes)", _bookingExpiryMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndCancelExpiredTickets();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Ticket Expiration Service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("⛔ Ticket Expiration Service stopped");
        }

        private async Task CheckAndCancelExpiredTickets()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;

            // ✅ TÌM TICKETS ĐÃ QUÁ 10 PHÚT KỂ TỪ LÚC BOOKING
            var expiredTickets = await context.Tickets
                .Include(t => t.Trip)
                .Include(t => t.Payment)
                .Where(t => 
                    t.Status == "Booked" && 
                    t.BookingDate.AddMinutes(_bookingExpiryMinutes) < now)
                .ToListAsync();

            if (expiredTickets.Count == 0)
            {
                _logger.LogInformation("ℹ️ No expired tickets found at {Time}", now);
                return;
            }

            _logger.LogInformation("🔍 Found {Count} expired tickets to cancel", expiredTickets.Count);

            foreach (var ticket in expiredTickets)
            {
                // Cập nhật ticket status
                ticket.Status = "Cancelled";

                // Cập nhật payment status nếu có
                if (ticket.Payment != null && ticket.Payment.Status == "Pending")
                {
                    ticket.Payment.Status = "Failed";
                }

                _logger.LogInformation(
                    "✅ Cancelled Ticket #{TicketId} - Booked at: {BookingDate}, Expired at: {ExpiryTime}", 
                    ticket.TicketId,
                    ticket.BookingDate,
                    ticket.BookingDate.AddMinutes(_bookingExpiryMinutes)
                );
            }

            // Lưu tất cả thay đổi
            var savedCount = await context.SaveChangesAsync();
            _logger.LogInformation("💾 Successfully cancelled {Count} tickets", savedCount);
        }
    }
}