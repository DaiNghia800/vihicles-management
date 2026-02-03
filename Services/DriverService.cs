using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Services
{
    public class DriverService : IDriverService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DriverService> _logger;

        public DriverService(ApplicationDbContext context, ILogger<DriverService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Driver CRUD Operations

        public async Task<IEnumerable<Driver>> GetAllDriversAsync()
        {
            try
            {
                return await _context.Drivers
                    .Include(d => d.User)
                        .ThenInclude(u => u.Role)
                    .Include(d => d.VehicleAssigned)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all drivers");
                throw;
            }
        }

        public async Task<Driver?> GetDriverByIdAsync(int id)
        {
            try
            {
                return await _context.Drivers
                    .Include(d => d.User)
                        .ThenInclude(u => u.Role)
                    .Include(d => d.VehicleAssigned)
                    .FirstOrDefaultAsync(d => d.DriverId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting driver with id {DriverId}", id);
                throw;
            }
        }

        public async Task<Driver> CreateDriverAsync(Driver driver)
        {
            try
            {
                // Validate: Check if user exists
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Uid == driver.UserId);
                
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // ✅ VALIDATE AGE - Must be 18+
                if (user.DateOfBirth.HasValue)
                {
                    var age = DateTime.Today.Year - user.DateOfBirth.Value.Year;
                    if (user.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                    
                    if (age < 18)
                    {
                        throw new InvalidOperationException($"Driver must be at least 18 years old. Selected user is only {age} years old.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("User date of birth is required to verify age requirement");
                }

                // Validate: Check if user has Driver role
                if (user.Role?.RoleName?.ToLower() != "driver")
                {
                    throw new InvalidOperationException("Selected user must have Driver role");
                }

                // Validate: Check if user already is a driver
                if (await IsUserAlreadyDriverAsync(driver.UserId))
                {
                    throw new InvalidOperationException("User is already assigned as a driver");
                }

                // Validate: Check license number uniqueness
                if (await ValidateLicenseNumberAsync(driver.LicenseNumber))
                {
                    throw new InvalidOperationException("License number already exists");
                }

                // Validate: Check license expiry date
                if (driver.LicenseExpiry.HasValue && driver.LicenseExpiry.Value <= DateTime.Now)
                {
                    throw new InvalidOperationException("License expiry date must be in the future");
                }

                driver.CreatedAt = DateTime.Now;
                driver.UpdatedAt = DateTime.Now;

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Driver created successfully with ID: {DriverId}", driver.DriverId);
                return driver;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating driver");
                throw;
            }
        }

        public async Task<Driver> UpdateDriverAsync(int id, Driver driver)
        {
            try
            {
                var existingDriver = await _context.Drivers.FindAsync(id);
                if (existingDriver == null)
                {
                    throw new KeyNotFoundException($"Driver with ID {id} not found");
                }

                // Validate: Check license number uniqueness (excluding current driver)
                if (await ValidateLicenseNumberAsync(driver.LicenseNumber, id))
                {
                    throw new InvalidOperationException("License number already exists");
                }

                // Validate: Check license expiry date
                if (driver.LicenseExpiry.HasValue && driver.LicenseExpiry.Value <= DateTime.Now)
                {
                    throw new InvalidOperationException("License expiry date must be in the future");
                }

                // Update properties
                existingDriver.LicenseNumber = driver.LicenseNumber;
                existingDriver.LicenseType = driver.LicenseType;
                existingDriver.LicenseExpiry = driver.LicenseExpiry;
                existingDriver.ExperienceYears = driver.ExperienceYears;
                existingDriver.Status = driver.Status;
                existingDriver.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Driver updated successfully with ID: {DriverId}", id);
                return existingDriver;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver with ID: {DriverId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDriverAsync(int id)
        {
            try
            {
                var driver = await _context.Drivers.FindAsync(id);
                if (driver == null)
                {
                    return false;
                }

                // Check if driver is assigned to any active trips
                var hasActiveTrips = await _context.Trips
                    .AnyAsync(t => t.DriverId == id && t.DepartureTime > DateTime.Now);

                if (hasActiveTrips)
                {
                    throw new InvalidOperationException("Cannot delete driver with active trips");
                }

                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Driver deleted successfully with ID: {DriverId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting driver with ID: {DriverId}", id);
                throw;
            }
        }

        #endregion

        #region User Related Operations

        public async Task<IEnumerable<Users>> GetAvailableUsersForDriverAsync()
        {
            try
            {
                // ✅ Lấy tất cả users có role Driver (không cần filter 18+, sẽ validate khi create)
                var driverRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.ToLower() == "driver" && !r.Deleted);

                if (driverRole == null)
                {
                    _logger.LogWarning("Driver role not found in the system");
                    return new List<Users>();
                }

                return await _context.Users
                    .Include(u => u.Role)
                    .Where(u => !u.Deleted 
                        && u.RoleUid == driverRole.Uid
                        && !_context.Drivers.Any(d => d.UserId == u.Uid))
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available users for driver");
                throw;
            }
        }

        #endregion

        #region License Management

        public async Task<IEnumerable<Driver>> GetDriversWithExpiringLicensesAsync(int days = 30)
        {
            try
            {
                var now = DateTime.Now;
                var futureDate = now.AddDays(days);

                return await _context.Drivers
                    .Include(d => d.User)
                        .ThenInclude(u => u.Role)
                    .Where(d => d.LicenseExpiry.HasValue &&
                               d.LicenseExpiry.Value >= now &&
                               d.LicenseExpiry.Value <= futureDate)
                    .OrderBy(d => d.LicenseExpiry)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drivers with expiring licenses");
                throw;
            }
        }

        public async Task<IEnumerable<Driver>> GetAllDriversSortedByLicenseExpiryAsync()
        {
            try
            {
                return await _context.Drivers
                    .Include(d => d.User)
                        .ThenInclude(u => u.Role)
                    .OrderBy(d => d.LicenseExpiry)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drivers sorted by license expiry");
                throw;
            }
        }

        #endregion

        #region Business Logic Validation

        public async Task<bool> IsUserAlreadyDriverAsync(int userId)
        {
            try
            {
                return await _context.Drivers.AnyAsync(d => d.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is already a driver");
                throw;
            }
        }

        public async Task<bool> ValidateLicenseNumberAsync(string licenseNumber, int? excludeDriverId = null)
        {
            try
            {
                var query = _context.Drivers.Where(d => d.LicenseNumber == licenseNumber);

                if (excludeDriverId.HasValue)
                {
                    query = query.Where(d => d.DriverId != excludeDriverId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating license number");
                throw;
            }
        }

        #endregion
    }
}