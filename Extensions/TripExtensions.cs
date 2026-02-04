using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;

namespace Public_Transport.Extensions
{
    public static class TripExtensions
    {
        /// <summary>
        /// Tính số chỗ đã được chiếm (Booked, Paid, Used)
        /// </summary>
        public static async Task<int> GetBookedSeatsCountAsync(this ApplicationDbContext context, int tripId)
        {
            return await context.Tickets
                .Where(t => t.TripId == tripId && 
                       (t.Status == "Booked" || t.Status == "Paid" || t.Status == "Used"))
                .CountAsync();
        }

        /// <summary>
        /// Tính số chỗ còn trống
        /// </summary>
        public static async Task<int> GetAvailableSeatsAsync(this ApplicationDbContext context, int tripId)
        {
            var trip = await context.Trips
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip?.Vehicle == null)
                return 0;

            var bookedSeats = await context.GetBookedSeatsCountAsync(tripId);
            return trip.Vehicle.SeatCapacity - bookedSeats;
        }

        /// <summary>
        /// Kiểm tra xem Trip còn chỗ không
        /// </summary>
        public static async Task<bool> HasAvailableSeatsAsync(this ApplicationDbContext context, int tripId, int seatsNeeded = 1)
        {
            var availableSeats = await context.GetAvailableSeatsAsync(tripId);
            return availableSeats >= seatsNeeded;
        }

        /// <summary>
        /// Kiểm tra xem có thể tạo ticket mới không (kiểm tra capacity)
        /// </summary>
        public static async Task<(bool CanBook, string Message)> CanBookTicketAsync(this ApplicationDbContext context, int tripId)
        {
            var trip = await context.Trips
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
                return (false, "Trip not found");

            if (trip.Vehicle == null)
                return (false, "Vehicle not assigned to this trip");

            var bookedSeats = await context.GetBookedSeatsCountAsync(tripId);
            var availableSeats = trip.Vehicle.SeatCapacity - bookedSeats;

            if (availableSeats <= 0)
                return (false, $"No seats available. All {trip.Vehicle.SeatCapacity} seats are booked.");

            return (true, $"{availableSeats} seats available");
        }
    }
}