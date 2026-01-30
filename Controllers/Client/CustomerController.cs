using Public_Transport.Models.ViewModels;
using Public_Transport.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Public_Transport.Controllers.Client
{
    [Authorize]
    [Route("/customer")]
    public class CustomerController : Controller
    {
        private readonly IUserService _userService;

        public CustomerController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpGet("get-my-profile")]
        public IActionResult GetMyProfile()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = _userService.GetUserById(currentUserId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return PartialView("~/Views/Customer/MyProfile.cshtml", user);
        }

        [HttpGet("profile/{id}")]
        public IActionResult ProfileSetting(int id)
        {
            var user = _userService.GetUserById(id);

            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var model = new UserCreateViewModel
            {
                Uid = user.Uid,
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                ImgUser = user.ImgUser,
                RoleUid = user.RoleUid
            };

            var allRoles = _userService.GetAllRoles();
            ViewData["RolesList"] = new SelectList(allRoles, "Uid", "RoleName", user.RoleUid);

            return View("~/Views/Customer/ProfileSetting.cshtml", model);
        }

        [HttpPost("profile/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(int id, [FromForm] UserCreateViewModel model)
        {
            if (id != model.Uid)
            {
                return BadRequest("ID không khớp");
            }

            // Nếu không nhập password mới, bỏ qua validation password
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove(nameof(model.Password));
                ModelState.Remove(nameof(model.ConfirmPassword));
            }

            // Tải lại RolesList phòng trường hợp phải trả về View
            var allRoles = _userService.GetAllRoles();
            ViewData["RolesList"] = new SelectList(allRoles, "Uid", "RoleName", model.RoleUid);

            if (!ModelState.IsValid)
            {
                return View("~/Views/Customer/ProfileSetting.cshtml", model);
            }

            // Gọi Service để update
            var (success, errorMessage) = await _userService.UpdateUserAsync(model);

            if (success)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("ProfileSetting", new { id = model.Uid });
            }
            else
            {
                ModelState.AddModelError(string.Empty, errorMessage);
                return View("~/Views/Customer/ProfileSetting.cshtml", model);
            }
        }
    }
}