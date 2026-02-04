using Microsoft.AspNetCore.Mvc;
using Public_Transport.Services.IServices;

namespace Public_Transport.Controllers.Client
{
    [Route("blogs")]
    public class BlogClientController : Controller
    {
        private readonly IBlogService _blogService;

        public BlogClientController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        // View: Danh sách blog
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Blog/blog-list.cshtml");
        }

        // View: Chi tiết blog
        [HttpGet("{id:int}")]
        public IActionResult Detail(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index");
            }

            ViewData["BlogId"] = id;
            return View("~/Views/Blog/blog-detail.cshtml");
        }

       
        [HttpGet("api/list")]
        public async Task<IActionResult> GetBlogsList()
        {
            var blogs = await _blogService.GetBlogsPublicAsync();
            return Ok(blogs);
        }

       
        [HttpGet("api/detail/{id:int}")]
        public async Task<IActionResult> GetBlogDetail(int id)
        {
            var blog = await _blogService.GetBlogDetailPublicAsync(id);
            if (blog == null)
            {
                return NotFound(new { message = "Blog not found" });
            }
            return Ok(blog);
        }

     
        [HttpGet("api/categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _blogService.GetCategoriesPublicAsync();
            return Ok(categories);
        }
    }
}