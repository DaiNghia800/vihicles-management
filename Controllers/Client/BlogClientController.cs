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

        // GET: /blogs
        [HttpGet("/blogs")]
        public IActionResult Index()
        {
            return View("~/Views/Blog/blog-list.cshtml");
        }

        // GET: /blogs/{id}
        [HttpGet("/blogs/{id:int}")]
        public IActionResult Detail(int id)
        {
            ViewData["BlogId"] = id;
            return View("~/Views/Blog/blog-detail.cshtml");
        }

        // === API ENDPOINTS ===

        // GET: /blogs/api/list
        [HttpGet("/blogs/api/list")]
        public async Task<IActionResult> GetBlogs()
        {
            try
            {
                var blogs = await _blogService.GetBlogsPublicAsync();
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading blogs", error = ex.Message });
            }
        }

        // GET: /blogs/api/detail/{id}
        [HttpGet("/blogs/api/detail/{id:int}")]
        public async Task<IActionResult> GetBlogDetail(int id)
        {
            try
            {
                var blog = await _blogService.GetBlogDetailPublicAsync(id);
                
                if (blog == null)
                {
                    return NotFound(new { message = "Blog not found" });
                }
                
                return Ok(blog);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading blog detail", error = ex.Message });
            }
        }

        // GET: /blogs/api/categories
        [HttpGet("/blogs/api/categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _blogService.GetCategoriesPublicAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading categories", error = ex.Message });
            }
        }
    }
}