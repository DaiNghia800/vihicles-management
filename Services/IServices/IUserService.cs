using Public_Transport.Models.ViewModels;
using Public_Transport.Helpers;
using Public_Transport.Models.Entities;

namespace Public_Transport.Services.IServices
{
    public interface IUserService
    {
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
    }
}

