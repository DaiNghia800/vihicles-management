using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Public_Transport.Services.IServices;

namespace Public_Transport.Controllers.Admin
{
    [Authorize(Policy = "NoCustomer")]
    [Route("/admin/user")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
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
    }
}
