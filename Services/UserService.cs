using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Helpers;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Models.ViewModels;
using Public_Transport.Services.IServices;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace Public_Transport.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IUploadService _uploadService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            IConfiguration configuration, 
            IUploadService uploadService,
            ILogger<UserService> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _uploadService = uploadService;
            _logger = logger;
        }

        public async Task<PaginatedList<Users>> GetAllUsersAsync(int pageIndex, int pageSize)
        {
            var query = _context.Users
                                .Where(u => u.Deleted == false)
                                .Include(u => u.Role)
                                .AsNoTracking()
                                .OrderBy(u => u.FullName);

            return await PaginatedList<Users>.CreateAsync(query, pageIndex, pageSize);
        }

        public Users GetUserById(int id)
        {
            try
            {
                return _context.Users
                               .Include(u => u.Role)
                               .AsNoTracking()
                               .FirstOrDefault(u => u.Uid == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return null;
            }
        }

        public async Task<bool> UpdateUser(Users userModel, List<IFormFile>? imgFiles)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(userModel.Uid);
                if (existingUser == null)
                {
                    return false;
                }

                // XỬ LÝ ẢNH
                var imageUrls = new List<string>();

                if (!string.IsNullOrEmpty(userModel.ImgUser))
                {
                    try
                    {
                        imageUrls = JsonSerializer.Deserialize<List<string>>(userModel.ImgUser) ?? new List<string>();
                    }
                    catch
                    {
                        imageUrls.Add(userModel.ImgUser);
                    }
                }

                if (imgFiles != null && imgFiles.Any())
                {
                    foreach (var file in imgFiles)
                    {
                        if (file.Length > 0)
                        {
                            var url = await _uploadService.UploadImageAsync(file);
                            imageUrls.Add(url);
                        }
                    }
                }

                existingUser.ImgUser = imageUrls.Count > 0 ? JsonSerializer.Serialize(imageUrls) : "[]";
                existingUser.FullName = userModel.FullName;
                existingUser.Email = userModel.Email;
                existingUser.PhoneNumber = userModel.PhoneNumber;
                existingUser.Address = userModel.Address;
                existingUser.RoleUid = userModel.RoleUid;
                existingUser.UpdatedAt = DateTime.Now;
                existingUser.UpdatedBy = "admin";

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userModel.Uid);
                return false;
            }
        }

        public List<Roles> GetAllRoles()
        {
            try
            {
                return _context.Roles.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return new List<Roles>();
            }
        }

        public async Task<bool> DeleteUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.Deleted = true;
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = "admin";
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CreateUser(UserCreateViewModel model)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Deleted == false);

                if (existingUser != null)
                {
                    if (existingUser.PasswordHash == null ||
                        existingUser.PasswordHash.StartsWith("EXTERNAL_LOGIN_"))
                    {
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        existingUser.FullName = model.FullName;
                        existingUser.PhoneNumber = model.PhoneNumber;
                        existingUser.Address = model.Address;
                        existingUser.RoleUid = model.RoleUid;
                        existingUser.UpdatedAt = DateTime.Now;
                        existingUser.UpdatedBy = "admin";

                        if (string.IsNullOrWhiteSpace(model.ImgUser) || model.ImgUser == "[]")
                        {
                            existingUser.ImgUser = WebConstants.DEFAULT_AVATAR;
                        }
                        else
                        {
                            existingUser.ImgUser = model.ImgUser;
                        }
                        await _context.SaveChangesAsync();

                        return (true, null);
                    }
                    else
                    {
                        return (false, "This email is already in use for a local account.");
                    }
                }

                var newUser = new Users
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    RoleUid = model.RoleUid,
                    ImgUser = string.IsNullOrWhiteSpace(model.ImgUser) || model.ImgUser == "[]" 
                        ? WebConstants.DEFAULT_AVATAR 
                        : model.ImgUser,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = "admin",
                    UpdatedBy = "admin",
                    Deleted = false
                };

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return (false, "A system error occurred. Please try again.");
            }
        }

        public Users Login(string username, string password)
        {
            try
            {
                var user = _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Email == username.ToLower().Trim() && u.Deleted == false);

                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", username);
                return null;
            }
        }

        public async Task<Users> FindOrCreateExternalUserAsync(
            string email,
            string fullName,
            string providerUserId,
            string provider)
        {
            try
            {
                var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Email == email && u.Deleted == false);

                if (user != null)
                {
                    return user;
                }

                // 2. Tạo user mới
                var passengerRole = await _context.Roles //  Đổi tên biến
                    .FirstOrDefaultAsync(r => r.RoleName == WebConstants.ROLE_PASSENGER); //  Đổi từ ROLE_CUSTOMER


                if (passengerRole == null)
                {
                    throw new Exception("Role 'Passenger' not found."); //  Đổi message
                }

                string createdByValue = provider switch
                {
                    "Google" => "GoogleAuth",
                    "Facebook" => "FacebookAuth",
                    _ => "ExternalAuth"
                };

                var newUser = new Users
                {
                    FullName = fullName ?? email,
                    Email = email,
                    RoleUid = passengerRole.Uid, //  Đổi tên biến

                    PasswordHash = $"EXTERNAL_LOGIN_{provider.ToUpper()}_{Guid.NewGuid()}",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = createdByValue,
                    UpdatedBy = createdByValue,
                    Deleted = false,
                    ImgUser = "[\"https://res.cloudinary.com/dfeaar87r/image/upload/v1763101391/default-avatar_uek2f1.png\"]"
                };

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

                newUser.Role = passengerRole; //  Đổi tên biến
                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FindOrCreateExternalUserAsync for email: {Email}", email);
                throw;
            }
        }

        public async Task<(Users User, string ErrorMessage)> RegisterUserAsync(RegisterViewModel model)
        {
            try
            {
                var existingUser = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Deleted == false);

                if (existingUser != null)
                {
                    if (existingUser.PasswordHash == null ||
                        existingUser.PasswordHash.StartsWith("EXTERNAL_LOGIN_"))
                    {
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        existingUser.FullName = model.FullName;
                        existingUser.UpdatedAt = DateTime.Now;
                        existingUser.UpdatedBy = "SelfRegister";

                        _context.Users.Update(existingUser);
                        await _context.SaveChangesAsync();

                        return (existingUser, null);
                    }
                    else
                    {
                        return (null, "This email is already in use.");
                    }
                }

                // 2. Tạo user mới
                var passengerRole = await _context.Roles //  Đổi tên biến
                    .FirstOrDefaultAsync(r => r.RoleName == WebConstants.ROLE_PASSENGER); //  Đổi từ ROLE_CUSTOMER

                if (passengerRole == null)
                {
                    return (null, "System error: Role 'Passenger' not found."); //  Đổi message
                }

                var newUser = new Users
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    RoleUid = passengerRole.Uid, //  Đổi tên biến
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = "SelfRegister",
                    UpdatedBy = "SelfRegister",
                    Deleted = false,
                    ImgUser = "[\"https://res.cloudinary.com/dfeaar87r/image/upload/v1763101391/default-avatar_uek2f1.png\"]"
                };

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

                newUser.Role = passengerRole;   
                return (newUser, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return (null, "A system error occurred. Please try again.");
            }
        }

        public async Task<(bool Success, string Message)> GenerateOtpAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.Deleted == false);

                if (user == null)
                {
                    return (false, "Email does not exist in the system");
                }

                var random = new Random();
                var otpCode = random.Next(100000, 999999).ToString();

                user.OtpCode = otpCode;
                user.OtpExpiry = DateTime.Now.AddMinutes(5);
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = "System";

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                await SendOtpEmailAsync(email, otpCode, user.FullName);

                return (true, "OTP code has been sent to your email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateOtpAsync for email: {Email}", email);
                return (false, "An error occurred while sending the OTP code");
            }
        }

        public async Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otpCode)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.Deleted == false);

                if (user == null)
                {
                    return (false, "Email does not exist");
                }

                if (string.IsNullOrEmpty(user.OtpCode))
                {
                    return (false, "No OTP code has been generated yet");
                }

                if (user.OtpExpiry < DateTime.Now)
                {
                    return (false, "OTP code has expired");
                }

                if (user.OtpCode != otpCode)
                {
                    return (false, "Incorrect OTP code");
                }

                return (true, "OTP authentication successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyOtpAsync for email: {Email}", email);
                return (false, "An error occurred while authenticating OTP");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string otpCode, string newPassword)
        {
            try
            {
                var (isValid, message) = await VerifyOtpAsync(email, otpCode);
                if (!isValid)
                {
                    return (false, message);
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.Deleted == false);

                if (user == null)
                {
                    return (false, "Email does not exist");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.OtpCode = null;
                user.OtpExpiry = null;
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = "System";

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return (true, "Password reset successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPasswordAsync for email: {Email}", email);
                return (false, "An error occurred while resetting your password.");
            }
        }

        private async Task SendOtpEmailAsync(string toEmail, string otpCode, string userName)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    Timeout = 30000,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "PM"),
                    Subject = "OTP code to reset password",
                    Body = $@"
                <html>
                    <body>
                    <h2>Hello {userName},</h2>
                    <p>You have requested a password reset. Your OTP code is:</p>
                    <h1 style='color: #4CAF50;'>{otpCode}</h1>
                    <p>This OTP code is valid for 5 minutes.</p>
                    <p>If you did not request a password reset, please ignore this email.</p>
                    </body>
                    </html>
                    ",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP email to: {Email}", toEmail);
                throw new Exception($"Error sending email: {ex.Message}", ex);
            }
        }

        public List<string> getPermissionRole(int roleId)
        {
            try
            {
                return _context.Permissions
                    .Include(p => p.PermissionType)
                    .Include(p => p.Function)
                    .Where(p => p.RoleId == roleId)
                    .Select(p => p.Function.Code + "_" + p.PermissionType.Code)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for role: {RoleId}", roleId);
                return new List<string>();
            }
        }

        public async Task<bool> UpdateProfile(ProfileUpdateViewModel model)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(model.Uid);
                if (existingUser == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(model.FullName))
                {
                    existingUser.FullName = model.FullName.Trim();
                }

                if (!string.IsNullOrEmpty(model.Email))
                {
                    existingUser.Email = model.Email;
                }

                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    existingUser.PhoneNumber = model.PhoneNumber;
                }

                if (!string.IsNullOrEmpty(model.Address))
                {
                    existingUser.Address = model.Address;
                }

                if (model.Photo != null && model.Photo.Length > 0)
                {
                    var imageUrls = new List<string>();

                    if (!string.IsNullOrEmpty(existingUser.ImgUser))
                    {
                        try
                        {
                            imageUrls = JsonSerializer.Deserialize<List<string>>(existingUser.ImgUser) ?? new List<string>();
                        }
                        catch
                        {
                            imageUrls.Add(existingUser.ImgUser);
                        }
                    }

                    var newUrl = await _uploadService.UploadImageAsync(model.Photo);
                    imageUrls.Add(newUrl);

                    existingUser.ImgUser = JsonSerializer.Serialize(imageUrls);
                }

                if (!string.IsNullOrEmpty(model.Password))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                existingUser.UpdatedAt = DateTime.Now;
                existingUser.UpdatedBy = existingUser.FullName;

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", model.Uid);
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateUserAsync(UserCreateViewModel model)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(model.Uid);
                if (existingUser == null)
                {
                    return (false, "User not found");
                }

                existingUser.ImgUser = string.IsNullOrWhiteSpace(model.ImgUser) || model.ImgUser == "[]"
                    ? WebConstants.DEFAULT_AVATAR
                    : model.ImgUser;

                existingUser.FullName = model.FullName;
                existingUser.Email = model.Email;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Address = model.Address;
                existingUser.RoleUid = model.RoleUid;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                existingUser.UpdatedAt = DateTime.Now;
                existingUser.UpdatedBy = "admin";

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", model.Uid);
                return (false, "A system error occurred. Please try again.");
            }
        }

        #region NEW METHODS for Admin User Management

        public async Task<(IEnumerable<Users> Users, int TotalCount)> GetUsersWithFiltersAsync(
            string? searchTerm,
            int? roleFilter,
            int pageIndex,
            int pageSize)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Role)
                    .Where(u => !u.Deleted)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(u =>
                        u.FullName.Contains(searchTerm) ||
                        u.Email.Contains(searchTerm) ||
                        u.PhoneNumber.Contains(searchTerm));
                }

                if (roleFilter.HasValue)
                {
                    query = query.Where(u => u.RoleUid == roleFilter.Value);
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (users, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users with filters");
                throw;
            }
        }

        public async Task<IEnumerable<Roles>> GetActiveRolesAsync()
        {
            try
            {
                return await _context.Roles
                    .Where(r => !r.Deleted)
                    .OrderBy(r => r.RoleName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active roles");
                throw;
            }
        }

        public async Task<Users?> GetUserByIdWithRoleAsync(int id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Uid == id && !u.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id with role: {UserId}", id);
                throw;
            }
        }

        // ✅ FIX: Thêm DateOfBirth vào CreateUserAdminAsync
        public async Task<Users> CreateUserAdminAsync(Users user, string password, string currentUserEmail)
        {
            try
            {
                if (await IsEmailExistsAsync(user.Email))
                {
                    throw new InvalidOperationException("Email already exists in the system");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException("Password is required");
                }

                if (!ValidatePassword(password))
                {
                    throw new InvalidOperationException("Password must be at least 6 characters, including uppercase, lowercase, number and special character");
                }

                // ✅ Validate DateOfBirth
                if (!user.DateOfBirth.HasValue)
                {
                    throw new InvalidOperationException("Date of birth is required");
                }

                if (user.DateOfBirth.Value > DateTime.Today)
                {
                    throw new InvalidOperationException("Date of birth cannot be in the future");
                }

                // Validate age (optional - tuỳ yêu cầu)
                var age = DateTime.Today.Year - user.DateOfBirth.Value.Year;
                if (user.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                
                if (age < 16)
                {
                    throw new InvalidOperationException("User must be at least 16 years old");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.CreatedAt = DateTime.Now;
                user.UpdatedAt = DateTime.Now;
                user.CreatedBy = currentUserEmail;
                user.UpdatedBy = currentUserEmail;
                user.Deleted = false;

                // ✅ DateOfBirth đã được gán từ model binding, không cần gán lại

                if (string.IsNullOrWhiteSpace(user.ImgUser))
                {
                    user.ImgUser = WebConstants.DEFAULT_AVATAR;
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully with ID: {UserId} by {CreatedBy}", user.Uid, currentUserEmail);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        // ✅ FIX: Thêm DateOfBirth vào UpdateUserAdminAsync
        public async Task<Users> UpdateUserAdminAsync(int id, Users model, string? newPassword, string currentUserEmail)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null || existingUser.Deleted)
                {
                    throw new KeyNotFoundException($"User with ID {id} not found");
                }

                if (await IsEmailExistsAsync(model.Email, id))
                {
                    throw new InvalidOperationException("Email already exists in the system");
                }

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (!ValidatePassword(newPassword))
                    {
                        throw new InvalidOperationException("Password must be at least 6 characters, including uppercase, lowercase, number and special character");
                    }
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                // ✅ Update DateOfBirth nếu có
                if (model.DateOfBirth.HasValue)
                {
                    if (model.DateOfBirth.Value > DateTime.Today)
                    {
                        throw new InvalidOperationException("Date of birth cannot be in the future");
                    }
                    existingUser.DateOfBirth = model.DateOfBirth;
                }

                existingUser.FullName = model.FullName;
                existingUser.Email = model.Email;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Address = model.Address;
                existingUser.RoleUid = model.RoleUid;
                existingUser.UpdatedAt = DateTime.Now;
                existingUser.UpdatedBy = currentUserEmail;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully with ID: {UserId} by {UpdatedBy}", id, currentUserEmail);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> SoftDeleteUserAsync(int id, string currentUserEmail)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null || user.Deleted)
                {
                    return false;
                }

                user.Deleted = true;
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = currentUserEmail;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User soft deleted with ID: {UserId} by {DeletedBy}", id, currentUserEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting user with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int id, string currentUserEmail)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return false;
                }

                user.Deleted = !user.Deleted;
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = currentUserEmail;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User status toggled for ID: {UserId}, New Status: {IsActive} by {UpdatedBy}", 
                    id, !user.Deleted, currentUserEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for ID: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Email == email && !u.Deleted);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Uid != excludeUserId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email exists");
                throw;
            }
        }

        public bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            var passwordRegex = new System.Text.RegularExpressions.Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$");
            return passwordRegex.IsMatch(password);
        }

        public Task CreateUserAsync(Users model)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}