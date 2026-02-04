using Microsoft.AspNetCore.Mvc;

namespace Public_Transport.Controllers.Client
{
    public class ContactController : Controller
    {
        [Route("/contact")]
        public IActionResult Index()
        {
            return View("~/Views/Contact/Index.cshtml");
        }
    }
}
