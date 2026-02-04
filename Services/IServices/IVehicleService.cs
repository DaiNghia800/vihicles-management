using Public_Transport.Models.Entities;
using System.Text.Json;

namespace Public_Transport.Services.IServices
{
    public interface IVehicleService
    {
        List<Vehicle> GetAllVehicle(int skip, int limitItem, string status, string keyword);
        int Count(string status, string keyword);
        void Create(Vehicle vehicle);
        Vehicle GetVehicle(int id);
        void Edit(int id, Vehicle data);
        void Delete(int id);
        string ChangeMulti(JsonElement data);
        
    }
}
