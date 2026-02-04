using Microsoft.AspNetCore.Mvc;

namespace Public_Transport.Controllers.Client
{
    public class AboutController : Controller
    {
        [Route("/about")]
        public IActionResult Index()
        {
            return View("~/Views/AboutUs/Index.cshtml");
        }
    }
}
