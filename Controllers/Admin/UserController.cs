using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;
using System.Security.Claims;
using Public_Transport.Helpers;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/user")]
    [Authorize(Policy = "NoCustomer")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly IUploadService _uploadService; // Inject the upload service

        public UserController(IUserService userService, ILogger<UserController> logger, IUploadService uploadService)
        {
            _userService = userService;
            _logger = logger;
            _uploadService = uploadService;
        }

        private string GetCurrentUserEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin";

        #region User CRUD

        // GET: /admin/user
        [HttpGet("")]
        public async Task<IActionResult> Index(string? searchTerm, int? roleFilter, int pageIndex = 1)
        {
            const int pageSize = 10;

            try
            {
                var (users, totalCount) = await _userService.GetUsersWithFiltersAsync(
                    searchTerm, roleFilter, pageIndex, pageSize);

                ViewBag.CurrentPage = pageIndex;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.TotalUsers = totalCount;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.RoleFilter = roleFilter;

                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;

                return View("~/Views/Admin/User/Index.cshtml", users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user index page");
                TempData["ErrorMessage"] = "An error occurred while loading the user list";
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
        public async Task<IActionResult> Create(Users model, string password, string confirmPassword, IFormFile? AvatarFile)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "Password is required";
                    return View("~/Views/Admin/User/Create.cshtml", model);
                }

                if (password != confirmPassword)
                {
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
                        if (uploadResult != null)
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
                    // Sử dụng avatar mặc định
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
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

        // POST: /admin/user/edit/{id}
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Users model, string? newPassword, string? confirmPassword, IFormFile? AvatarFile)
        {
            if (id != model.Uid)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword != confirmPassword)
                {
                    ModelState.AddModelError("confirmPassword", "Password confirmation does not match");
                }
            }

            if (!ModelState.IsValid)
            {
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
                return View("~/Views/Admin/User/Edit.cshtml", model);
            }

            try
            {
                // ✅ Upload avatar nếu có file mới
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    try
                    {
                        var uploadResult = await _uploadService.UploadSingleImageAsync(AvatarFile);
                        if (uploadResult != null)
                        {
                            model.ImgUser = uploadResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading avatar");
                        TempData["ErrorMessage"] = "Error uploading avatar image";
                        var roles = await _userService.GetActiveRolesAsync();
                        ViewBag.Roles = roles;
                        return View("~/Views/Admin/User/Edit.cshtml", model);
                    }
                }

                await _userService.UpdateUserAdminAsync(id, model, newPassword, GetCurrentUserEmail());
                TempData["SuccessMessage"] = "User information updated successfully!";
                return RedirectToAction("Index");
            }
            catch (KeyNotFoundException)
            {
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
                return View("~/Views/Admin/User/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
                ModelState.AddModelError("", "An error occurred while updating the user");
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
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