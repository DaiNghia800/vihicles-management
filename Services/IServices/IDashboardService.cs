using Public_Transport.Models.DTO;
using Public_Transport.Models.Entities;

namespace Public_Transport.Services.IServices
{
    public interface IDashboardService
    {
        int getVehicleActive();
        int getDailyPassengers();
        int getOperatingTripsToday();
        List<TrafficFlowDTO> GetTrafficFlow();
    }
}
