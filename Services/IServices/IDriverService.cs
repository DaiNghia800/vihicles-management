using Public_Transport.Models.Entities;

namespace Public_Transport.Services.IServices
{
    public interface IDriverService
    {
        // Driver CRUD operations
        Task<IEnumerable<Driver>> GetAllDriversAsync();
        Task<Driver?> GetDriverByIdAsync(int id);
        Task<Driver> CreateDriverAsync(Driver driver);
        Task<Driver> UpdateDriverAsync(int id, Driver driver);
        Task<bool> DeleteDriverAsync(int id);

        // User related operations
        Task<IEnumerable<Users>> GetAvailableUsersForDriverAsync();

        // License management
        Task<IEnumerable<Driver>> GetDriversWithExpiringLicensesAsync(int days = 30);
        Task<IEnumerable<Driver>> GetAllDriversSortedByLicenseExpiryAsync();

        // Business logic
        Task<bool> IsUserAlreadyDriverAsync(int userId);
        Task<bool> ValidateLicenseNumberAsync(string licenseNumber, int? excludeDriverId = null);
    }
}