using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/driver")]
    [Authorize(Policy = "NoCustomer")]
    public class DriverController : Controller
    {
        private readonly IDriverService _driverService;
        private readonly ILogger<DriverController> _logger;

        public DriverController(IDriverService driverService, ILogger<DriverController> logger)
        {
            _driverService = driverService;
            _logger = logger;
        }

        #region Driver CRUD

        // GET: /admin/driver
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var drivers = await _driverService.GetAllDriversAsync();
                return View("~/Views/Admin/Driver/Index.cshtml", drivers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver index page");
                TempData["ErrorMessage"] = "An error occurred while loading the driver list";
                return View("~/Views/Admin/Driver/Index.cshtml", new List<Driver>());
            }
        }

        // GET: /admin/driver/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var availableUsers = await _driverService.GetAvailableUsersForDriverAsync();
                ViewBag.Users = availableUsers;
                
                if (!availableUsers.Any())
                {
                    TempData["WarningMessage"] = "No eligible users found. Users must have Driver role and be at least 18 years old.";
                }
                
                return View("~/Views/Admin/Driver/Create.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver create page");
                TempData["ErrorMessage"] = "An error occurred while loading the create driver page";
                return RedirectToAction("Index");
            }
        }

        // POST: /admin/driver/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Driver model)
        {
            if (!ModelState.IsValid)
            {
                var users = await _driverService.GetAvailableUsersForDriverAsync();
                ViewBag.Users = users;
                return View("~/Views/Admin/Driver/Create.cshtml", model);
            }

            try
            {
                await _driverService.CreateDriverAsync(model);
                TempData["SuccessMessage"] = "Driver added successfully!";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var users = await _driverService.GetAvailableUsersForDriverAsync();
                ViewBag.Users = users;
                return View("~/Views/Admin/Driver/Create.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating driver");
                ModelState.AddModelError("", "An error occurred while creating the driver");
                var users = await _driverService.GetAvailableUsersForDriverAsync();
                ViewBag.Users = users;
                return View("~/Views/Admin/Driver/Create.cshtml", model);
            }
        }

        // GET: /admin/driver/edit/{id}
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var driver = await _driverService.GetDriverByIdAsync(id);
                if (driver == null)
                {
                    TempData["ErrorMessage"] = "Driver not found";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Admin/Driver/Edit.cshtml", driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver edit page for ID: {DriverId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the edit page";
                return RedirectToAction("Index");
            }
        }

        // POST: /admin/driver/edit/{id}
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Driver model)
        {
            if (id != model.DriverId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Driver/Edit.cshtml", model);
            }

            try
            {
                await _driverService.UpdateDriverAsync(id, model);
                TempData["SuccessMessage"] = "Driver information updated successfully!";
                return RedirectToAction("Index");
            }
            catch (KeyNotFoundException)
            {
                TempData["ErrorMessage"] = "Driver not found";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("~/Views/Admin/Driver/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver with ID: {DriverId}", id);
                ModelState.AddModelError("", "An error occurred while updating the driver");
                return View("~/Views/Admin/Driver/Edit.cshtml", model);
            }
        }

        // POST: /admin/driver/delete/{id}
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _driverService.DeleteDriverAsync(id);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Driver not found!";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xóa tài xế thành công!";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting driver with ID: {DriverId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tài xế";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region License Management

        // GET: /admin/driver/license-management
        [HttpGet("license-management")]
        public async Task<IActionResult> LicenseManagement()
        {
            try
            {
                var allDrivers = await _driverService.GetAllDriversSortedByLicenseExpiryAsync();
                var expiringDrivers = await _driverService.GetDriversWithExpiringLicensesAsync(30);

                ViewBag.ExpiringDrivers = expiringDrivers;

                return View("~/Views/Admin/Driver/LicenseManagement.cshtml", allDrivers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading license management page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang quản lý giấy phép";
                return RedirectToAction("Index");
            }
        }

        #endregion
    }
}