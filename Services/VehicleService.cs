using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;
using System.Text.Json;

namespace Public_Transport.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;

        public VehicleService(ApplicationDbContext context) 
        {
            _context = context;
        }

        public List<Vehicle> GetAllVehicle(int skip, int limitItem, string status, string keyword)
        {
            try
            {
                var query = _context.Vehicles
                            .Where(p => !p.Deleted);

                if (!string.IsNullOrWhiteSpace(keyword))
                    query = query.Where(p => p.LicensePlate.Contains(keyword));

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                return query
                    .OrderByDescending(p => p.VehicleId)
                    .Skip(skip)
                    .Take(limitItem)
                    .ToList();

            }
            catch (Exception ex)
            {
                return new List<Vehicle>();
            }
        }

        public int Count(string status, string keyword)
        {
            try
            {
                var query = _context.Vehicles.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                    query = query.Where(p => p.LicensePlate.Contains(keyword));

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                query = query.Where(p => !p.Deleted);
                return query.Count();
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public void Create(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);

            _context.SaveChanges();
        }

        public Vehicle GetVehicle(int id)
        {
            try
            {
                return _context.Vehicles.SingleOrDefault(p => p.VehicleId == id && !p.Deleted);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void Edit(int id, Vehicle data)
        {
            try
            {
                var vehicle = _context.Vehicles.SingleOrDefault(p => p.VehicleId == id && !p.Deleted);

                if (vehicle != null)
                {
                    vehicle.LicensePlate = data.LicensePlate.Trim();
                    vehicle.VehicleType = data.VehicleType.Trim();
                    vehicle.SeatCapacity = data.SeatCapacity;
                    vehicle.Thumbnail = data.Thumbnail;
                    vehicle.Status = data.Status;
                    vehicle.UpdatedAt = DateTime.Now;

                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                var vehicle = _context.Vehicles.SingleOrDefault(p => p.VehicleId == id && !p.Deleted);
                vehicle.Deleted = true;
                vehicle.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string ChangeMulti(JsonElement data)
        {
            try
            {
                var ids = data.GetProperty("id").EnumerateArray()
                    .Select(x =>
                    {
                        if (x.ValueKind == JsonValueKind.String)
                            return int.Parse(x.GetString());
                        else
                            return x.GetInt32();
                    })
                    .ToList();
                string status = data.GetProperty("status").GetString();
                var vehicles = _context.Vehicles.Where(p => ids.Contains(p.VehicleId)).ToList();
                switch (status)
                {
                    case "Active":
                    case "Inactive":
                    case "Maintenance":
                        foreach (var p in vehicles)
                        {
                            p.Status = status;
                            p.UpdatedAt = DateTime.Now;
                        }
                        _context.SaveChanges();
                        return "success";
                    case "delete":
                        foreach (var p in vehicles)
                        {
                            p.Deleted = true;
                            p.UpdatedAt = DateTime.Now;
                        }

                        _context.SaveChanges();
                        return "deleted";
                    default:
                        return "invalid";
                }


            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
