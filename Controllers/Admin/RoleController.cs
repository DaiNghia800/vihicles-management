using Microsoft.AspNetCore.Mvc;
using Public_Transport.Services.IServices;

namespace Public_Transport.Controllers.Admin
{

    [Route("/admin/role")]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }
    

        [HttpGet("")]
        public IActionResult Index()
        {
            var listRole = _roleService.GetAllRole();
            ViewData["listRole"] = listRole;

            return View("~/Views/Admin/Role/Index.cshtml");
        }
    }
}
