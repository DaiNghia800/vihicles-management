using Public_Transport.Models.ViewModels;
using Public_Transport.Helpers;
using Public_Transport.Models.Entities;

namespace Public_Transport.Services.IServices
{
    public interface IUserService
    {
        // Existing methods
        List<Roles> GetAllRoles();
        Task<PaginatedList<Users>> GetAllUsersAsync(int pageIndex, int pageSize);
        Users GetUserById(int userId);
        Task<bool> UpdateUser(Users userToUpdate, List<IFormFile>? imgFiles);
        Task<bool> DeleteUser(int userId);
        Task<(bool Success, string ErrorMessage)> CreateUser(UserCreateViewModel model);
        Users Login(string username, string password);
        Task<Users> FindOrCreateExternalUserAsync(string email, string fullName, string providerUserId, string provider);
        Task<(Users User, string ErrorMessage)> RegisterUserAsync(RegisterViewModel model);
        Task<(bool Success, string Message)> GenerateOtpAsync(string email);
        Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otpCode);
        Task<(bool Success, string Message)> ResetPasswordAsync(string email, string otpCode, string newPassword);
        List<string> getPermissionRole(int roleId);
        Task<bool> UpdateProfile(ProfileUpdateViewModel model);
        Task<(bool Success, string ErrorMessage)> UpdateUserAsync(UserCreateViewModel model);

        // NEW METHODS for Admin User Management
        Task<(IEnumerable<Users> Users, int TotalCount)> GetUsersWithFiltersAsync(
            string? searchTerm, 
            int? roleFilter, 
            int pageIndex, 
            int pageSize);
        
        Task<IEnumerable<Roles>> GetActiveRolesAsync();
        Task<Users?> GetUserByIdWithRoleAsync(int id);
        Task<Users> CreateUserAdminAsync(Users user, string password, string currentUserEmail);
        Task<Users> UpdateUserAdminAsync(int id, Users model, string? newPassword, string currentUserEmail);
        Task<bool> SoftDeleteUserAsync(int id, string currentUserEmail);
        Task<bool> ToggleUserStatusAsync(int id, string currentUserEmail);
        
        // Validation methods
        Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null);
        bool ValidatePassword(string password);
    }
}

