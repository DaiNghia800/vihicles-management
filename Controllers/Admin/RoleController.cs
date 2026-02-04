using Microsoft.AspNetCore.Mvc;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;
using System.Data;
using System.Text.Json;

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

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Role/Create.cshtml");
        }

        [HttpPost("create")]
        public IActionResult CreatePost([FromForm] Roles roles)
        {
            if (!ModelState.IsValid || _roleService.checkRoleName(-1, roles.RoleName))
            {
                if (_roleService.checkRoleName(-1, roles.RoleName))
                {
                    ModelState.AddModelError("RoleName", "Vai trò này đã tồn tại, vui nhập lòng nhập vai trò mới");
                }
                return View("~/Views/Admin/Role/Create.cshtml", roles);
            }

            roles.CreatedAt = DateTime.Now;
            roles.UpdatedAt = DateTime.Now;
            _roleService.CreateRole(roles);

            return Redirect("/admin/role");
        }

        [HttpGet("edit/{id}")]
        public IActionResult Edit(int id)
        {
            var role = _roleService.GetRole(id);
            ViewData["role"] = role;
            return View("~/Views/Admin/Role/Edit.cshtml", role);
        }

        [HttpPost("edit/{id}")]
        public IActionResult CreatePost([FromForm] Roles roles, int id)
        {
            if (!ModelState.IsValid || _roleService.checkRoleName(id, roles.RoleName))
            {
                if (_roleService.checkRoleName(id, roles.RoleName))
                {
                    ModelState.AddModelError("RoleName", "Vai trò này đã tồn tại, vui nhập lòng nhập vai trò mới");
                }
                var role = _roleService.GetRole(id);
                ViewData["role"] = role;
                return View("~/Views/Admin/Role/Edit.cshtml", roles);
            }

            _roleService.EditRole(roles, id);

            return Redirect($"/admin/role/edit/{id}");
        }

        [HttpPost("delete/{id}")]
        public JsonResult Delete(int id)
        {
            int role = _roleService.DeleteRole(id);
            return Json(new
            {
                code = role,
                message = $"Unable to delete the role of the game containing user {role}. Please delete or transfer the previous user."
            });
        }

        [HttpGet("permission")]
        public IActionResult Permission()
        {
            var listRole = _roleService.GetRolePermission();
            var permissions = _roleService.GetPermissions();
            ViewData["roles"] = listRole;
            ViewData["permissions"] = permissions;
            return View("~/Views/Admin/Role/Permission.cshtml");
        }

        [HttpPost("permission")]
        public JsonResult PremissionPost([FromBody] JsonElement data)
        {
            _roleService.UpdatePermission(data);
            return Json(new { code = "success" });
        }
    }
}
