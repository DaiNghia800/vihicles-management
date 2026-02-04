using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Public_Transport.Helpers;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;
using System.Text.Json;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/user")]
    [Authorize(Policy = "NoPassenger")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUploadService _uploadService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IUploadService uploadService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _uploadService = uploadService;
            _logger = logger;
        }

        // ✅ Helper method to get current user email
        private string GetCurrentUserEmail()
        {
            return User?.Identity?.Name ?? "admin";
        }

        #region User Management CRUD

        // GET: /admin/user
        [HttpGet("")]
        public async Task<IActionResult> Index(string searchTerm, int? roleFilter, int pageIndex = 1)
        {
            try
            {
                const int pageSize = 10;
                var (users, totalCount) = await _userService.GetUsersWithFiltersAsync(searchTerm, roleFilter, pageIndex, pageSize);
                var roles = await _userService.GetActiveRolesAsync();

                ViewBag.Roles = roles;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.RoleFilter = roleFilter;
                ViewBag.CurrentPage = pageIndex;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.TotalUsers = totalCount;

                return View("~/Views/Admin/User/Index.cshtml", users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user index");
                TempData["ErrorMessage"] = "An error occurred while loading users";
                return View("~/Views/Admin/User/Index.cshtml", new List<Users>());
            }
        }

        // GET: /admin/user/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
                return View("~/Views/Admin/User/Create.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user create page");
                TempData["ErrorMessage"] = "An error occurred while loading the create user page";
                return RedirectToAction("Index");
            }
        }

        // POST: /admin/user/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Users model, string password, string confirmPassword, IFormFile AvatarFile)
        {
            try
            {
                // ✅ Load roles trước khi validate
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;

                // Validation
                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("Password", "Password is required");
                    TempData["ErrorMessage"] = "Password is required";
                    return View("~/Views/Admin/User/Create.cshtml", model);
                }

                if (password != confirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Password and confirm password do not match");
                    TempData["ErrorMessage"] = "Password and confirm password do not match";
                    return View("~/Views/Admin/User/Create.cshtml", model);
                }

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please fill in all required fields correctly";
                    return View("~/Views/Admin/User/Create.cshtml", model);
                }

                // ✅ Upload avatar nếu có
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    try
                    {
                        var uploadResult = await _uploadService.UploadSingleImageAsync(AvatarFile);
                        if (!string.IsNullOrEmpty(uploadResult))
                        {
                            model.ImgUser = uploadResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading avatar");
                        TempData["ErrorMessage"] = "Error uploading avatar image";
                        return View("~/Views/Admin/User/Create.cshtml", model);
                    }
                }
                else
                {
                    // ✅ FIX: Sử dụng WebConstants.DEFAULT_AVATAR
                    model.ImgUser = WebConstants.DEFAULT_AVATAR;
                }

                // Hash password
                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Set metadata
                var currentUserEmail = GetCurrentUserEmail();
                model.CreatedBy = currentUserEmail;
                model.UpdatedBy = currentUserEmail;
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.Deleted = false;

                // Create user
                await _userService.CreateUserAsync(model);

                TempData["SuccessMessage"] = "User created successfully!";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
                
                _logger.LogError(ex, "Validation error creating user");
                TempData["ErrorMessage"] = ex.Message;
                return View("~/Views/Admin/User/Create.cshtml", model);
            }
            catch (Exception ex)
            {
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
                
                _logger.LogError(ex, "Error creating user");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View("~/Views/Admin/User/Create.cshtml", model);
            }
        }

        // GET: /admin/user/edit/{id}
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdWithRoleAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction("Index");
                }

                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;

                return View("~/Views/Admin/User/Edit.cshtml", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user edit page for ID: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the edit page";
                return RedirectToAction("Index");
            }
        }

        // GET: /admin/user/get-user-detail
        [HttpGet("get-user-detail")]
        public IActionResult GetUserDetail(int id)
        {
            var user = _userService.GetUserById(id);

            if (user == null)
            {
                return NotFound("User not found");
            }
            return PartialView("~/Views/Admin/User/userdetail.cshtml", user);
        }

        // POST: /admin/user/edit/{id}
        // POST: /admin/user/edit/{id}
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Users model, string newPassword, string confirmPassword, IFormFile AvatarFile)
        {
            if (id != model.Uid)
            {
                return NotFound();
            }

            // ✅ Load roles trước để có thể return về view nếu có lỗi
            var roles = await _userService.GetActiveRolesAsync();
            ViewBag.Roles = roles;

            try
            {
                // ✅ LOG 1: Check file có được gửi lên không
                _logger.LogInformation("=== EDIT USER {UserId} ===", id);
                _logger.LogInformation("AvatarFile: {HasFile}", AvatarFile != null ? $"YES - {AvatarFile.FileName} ({AvatarFile.Length} bytes)" : "NO");
                _logger.LogInformation("Model.ImgUser before processing: {ImgUser}", model.ImgUser);

                // ✅ FIX 1: Lấy user hiện tại TRƯỚC để giữ lại ảnh cũ
                var existingUser = await _userService.GetUserByIdWithRoleAsync(id);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("Existing user ImgUser: {ExistingImg}", existingUser.ImgUser);

                // ✅ FIX 2: XỬ LÝ AVATAR FILE TRƯỚC - KHÔNG phụ thuộc vào ModelState
                string avatarUrl = existingUser.ImgUser; // Giữ ảnh cũ làm default

                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    _logger.LogInformation("Processing avatar upload...");

                    // Validate file size (5MB)
                    if (AvatarFile.Length > 5 * 1024 * 1024)
                    {
                        _logger.LogWarning("Avatar file too large: {Size} bytes", AvatarFile.Length);
                        TempData["ErrorMessage"] = "Avatar file must be less than 5MB";
                        return View("~/Views/Admin/User/Edit.cshtml", model);
                    }

                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        _logger.LogWarning("Invalid file type: {Extension}", fileExtension);
                        TempData["ErrorMessage"] = "Only JPG, PNG, and GIF images are allowed";
                        return View("~/Views/Admin/User/Edit.cshtml", model);
                    }

                    try
                    {
                        // ✅ Upload và lấy URL
                        avatarUrl = await _uploadService.UploadSingleImageAsync(AvatarFile);
                        _logger.LogInformation("✅ Avatar uploaded successfully: {Url}", avatarUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error uploading avatar");
                        TempData["ErrorMessage"] = "Error uploading avatar image: " + ex.Message;
                        return View("~/Views/Admin/User/Edit.cshtml", model);
                    }
                }
                else
                {
                    _logger.LogInformation("No new avatar file, keeping existing: {Existing}", avatarUrl);
                }

                // ✅ FIX 3: GÁN avatar URL vào model TRƯỚC KHI validate
                model.ImgUser = avatarUrl;
                _logger.LogInformation("Model.ImgUser after processing: {ImgUser}", model.ImgUser);

                // ✅ Validate password nếu có
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (newPassword != confirmPassword)
                    {
                        TempData["ErrorMessage"] = "Password and confirmation password do not match";
                        return View("~/Views/Admin/User/Edit.cshtml", model);
                    }
                }

                // ✅ FIX 4: BỎ ModelState validation vì có thể có lỗi không liên quan
                // Thay vào đó, validate từng field cụ thể
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    TempData["ErrorMessage"] = "Full name is required";
                    return View("~/Views/Admin/User/Edit.cshtml", model);
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    TempData["ErrorMessage"] = "Email is required";
                    return View("~/Views/Admin/User/Edit.cshtml", model);
                }

                if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    TempData["ErrorMessage"] = "Phone number is required";
                    return View("~/Views/Admin/User/Edit.cshtml", model);
                }

                if (model.RoleUid <= 0)
                {
                    TempData["ErrorMessage"] = "Please select a role";
                    return View("~/Views/Admin/User/Edit.cshtml", model);
                }

                // ✅ Update user với avatar URL đã được xử lý
                _logger.LogInformation("Calling UpdateUserAdminAsync with ImgUser: {ImgUser}", model.ImgUser);
                await _userService.UpdateUserAdminAsync(id, model, newPassword, GetCurrentUserEmail());

                _logger.LogInformation("✅ User {UserId} updated successfully", id);
                TempData["SuccessMessage"] = "User information updated successfully!";
                return RedirectToAction("Index");
            }
            catch (KeyNotFoundException)
            {
                _logger.LogError("User {UserId} not found", id);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Validation error updating user {UserId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return View("~/Views/Admin/User/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the user: " + ex.Message;
                return View("~/Views/Admin/User/Edit.cshtml", model);
            }
        }
        // GET: /admin/user/detail/{id}
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdWithRoleAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Admin/User/Detail.cshtml", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user detail for ID: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading user details";
                return RedirectToAction("Index");
            }
        }

        // POST: /admin/user/delete/{id}
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _userService.SoftDeleteUserAsync(id, GetCurrentUserEmail());
                if (!success)
                {
                    TempData["ErrorMessage"] = "User not found!";
                }
                else
                {
                    TempData["SuccessMessage"] = "User deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the user";
            }

            return RedirectToAction("Index");
        }

        // POST: /admin/user/toggle-status/{id}
        [HttpPost("toggle-status/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var success = await _userService.ToggleUserStatusAsync(id, GetCurrentUserEmail());
                if (!success)
                {
                    return Json(new { success = false, message = "User not found!" });
                }

                var user = await _userService.GetUserByIdWithRoleAsync(id);
                return Json(new
                {
                    success = true,
                    message = user.Deleted ? "User has been deactivated" : "User has been activated",
                    isActive = !user.Deleted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for ID: {UserId}", id);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        #endregion
    }
}

