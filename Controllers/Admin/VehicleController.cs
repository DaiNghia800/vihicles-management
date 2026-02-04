using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Logging;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;
using System.Text.Json;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/vehicle")]
    public class VehicleController : Controller
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            //search
            string keyword = Request.Query["keyword"];
            //end search

            //filter
            string status = Request.Query["status"];
            if (string.IsNullOrEmpty(status))
            {
                status = null;
            }
            //end filter

            //pagination
            string pageStr = Request.Query["page"];
            int page = 1;
            int limitItem = 5;
            if (!string.IsNullOrEmpty(pageStr))
            {
                int.TryParse(pageStr, out page);
            }

            int skip = (page - 1) * limitItem;
            int totalProduct = _vehicleService.Count(status, keyword);
            int totalPage = (int)Math.Ceiling((double)totalProduct / limitItem);
            //pagination

            var listVehicle = _vehicleService.GetAllVehicle(skip, limitItem, status, keyword);
            ViewData["Vehicles"] = listVehicle;
            ViewData["TotalPage"] = totalPage;
            ViewData["CurrentPage"] = page;
            return View("~/Views/Admin/Vehicle/Index.cshtml");
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Vehicle/Create.cshtml");
        }

        [HttpPost("create")]
        public IActionResult CreatePost([FromForm] Vehicle vehicle)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Vehicle/Create.cshtml", vehicle);
            }

            vehicle.LicensePlate = vehicle.LicensePlate.Trim();
            vehicle.VehicleType = vehicle.VehicleType.Trim();
            vehicle.CreatedAt = DateTime.Now;
            vehicle.UpdatedAt = DateTime.Now;

            _vehicleService.Create(vehicle);
            return Redirect("/admin/vehicle");
        }

        [HttpGet("edit/{id}")]
        public IActionResult Edit(int id)
        {
            var vehicle = _vehicleService.GetVehicle(id);
            ViewData["vehicle"] = vehicle;
            return View("~/Views/Admin/Vehicle/Edit.cshtml", vehicle);
        }

        [HttpPost("edit/{id}")]
        public IActionResult EditPost([FromForm] Vehicle data, int id)
        {
            if (!ModelState.IsValid)
            {
                var vehicle = _vehicleService.GetVehicle(id);
                ViewData["vehicle"] = vehicle;
                return View("~/Views/Admin/Vehicle/Edit.cshtml", data);
            }

            _vehicleService.Edit(id, data);
            return Redirect($"/admin/vehicle/edit/{id}");
        }

        [HttpPost("delete/{id}")]
        public JsonResult Delete(int id)
        {
            _vehicleService.Delete(id);
            return Json(new { code = 0 });
        }

        [HttpPost("change-multi")]
        public JsonResult ChangeMulti([FromBody] JsonElement data)
        {
            string result = _vehicleService.ChangeMulti(data);
            return Json(new { code = result });
        }

    }
}
