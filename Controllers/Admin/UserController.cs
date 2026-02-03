using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;
using System.Security.Claims;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/user")]
    [Authorize(Policy = "NoCustomer")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
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
        public async Task<IActionResult> Create(Users model, string password, string confirmPassword)
        {
            try
            {
                // ✅ FIX 1: Validate required fields
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ModelState.AddModelError("FullName", "Full name is required");
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "Email is required");
                }

                if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number is required");
                }

                // ✅ FIX 2: Validate RoleUid
                if (model.RoleUid <= 0)
                {
                    ModelState.AddModelError("RoleUid", "Please select a valid role");
                }

                // ✅ FIX 3: Validate passwords
                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("password", "Password is required");
                }
                else if (password != confirmPassword)
                {
                    ModelState.AddModelError("confirmPassword", "Password confirmation does not match");
                }

                // ✅ FIX 4: Validate DateOfBirth
                if (!model.DateOfBirth.HasValue)
                {
                    ModelState.AddModelError("DateOfBirth", "Date of birth is required");
                }
                else if (model.DateOfBirth.Value > DateTime.Today)
                {
                    ModelState.AddModelError("DateOfBirth", "Date of birth cannot be in the future");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create user validation failed. Errors: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                    var roles = await _userService.GetActiveRolesAsync();
                    ViewBag.Roles = roles;
                    return View("~/Views/Admin/User/Create.cshtml", model);
                }

                // ✅ Create user
                await _userService.CreateUserAdminAsync(model, password, GetCurrentUserEmail());
                
                TempData["SuccessMessage"] = "User created successfully!";
                _logger.LogInformation("User created successfully: {Email} by {Creator}", model.Email, GetCurrentUserEmail());
                
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while creating user");
                ModelState.AddModelError("", ex.Message);
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
                return View("~/Views/Admin/User/Create.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating user");
                ModelState.AddModelError("", "An unexpected error occurred while creating the user. Please try again.");
                var roles = await _userService.GetActiveRolesAsync();
                ViewBag.Roles = roles;
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
        public async Task<IActionResult> Edit(int id, Users model, string? newPassword, string? confirmPassword)
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