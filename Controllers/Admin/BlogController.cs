using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Public_Transport.Models.DTO;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;
using System.Security.Claims;

namespace Public_Transport.Controllers.Admin
{
    public class BlogController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly IUploadService _uploadService;

        public BlogController(IBlogService blogService, IUploadService uploadService)
        {
            _blogService = blogService;
            _uploadService = uploadService;
        }

        // === PUBLIC ROUTES (đặt TRƯỚC các route admin để tránh conflict) ===
        [HttpGet("blog")]
        public IActionResult Index()
        {
            return View("~/Views/Blog/blog-list.cshtml");
        }

        [HttpGet("blog/{id:int}")] // Thêm constraint :int để chỉ match với số
        public IActionResult Detail(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index");
            }

            ViewData["BlogId"] = id;
            return View("~/Views/Blog/blog-detail.cshtml");
        }

        [HttpGet("blog/api/list")]
        public async Task<IActionResult> GetBlogsPublic()
        {
            var blogs = await _blogService.GetBlogsPublicAsync();
            return Ok(blogs);
        }

        [HttpGet("blog/api/detail/{id:int}")]
        public async Task<IActionResult> GetBlogDetailPublic(int id)
        {
            var blog = await _blogService.GetBlogDetailPublicAsync(id);
            if (blog == null)
            {
                return NotFound(new { message = "Blog not found" });
            }
            return Ok(blog);
        }

        [HttpGet("blog/api/categories")]
        public async Task<IActionResult> GetCategoriesPublic()
        {
            var categories = await _blogService.GetCategoriesPublicAsync();
            return Ok(categories);
        }

        // === ADMIN ROUTES ===
        [HttpGet("admin/blog")]
        [Authorize(Policy = "NoCustomer")]
        public IActionResult IndexAdmin()
        {
            return View("~/Views/Admin/Blog/Index.cshtml");
        }

        [HttpGet("admin/blog/create")]
        [Authorize(Policy = "NoCustomer")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Blog/Create.cshtml");
        }

        [HttpGet("admin/blog/edit/{id:int}")]
        [Authorize(Policy = "NoCustomer")]
        public IActionResult Edit(int id)
        {
            ViewData["BlogId"] = id;
            return View("~/Views/Admin/Blog/Create.cshtml");
        }

        [HttpPost("admin/blog/api/upload-image")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file type. Only images are allowed." });
            }
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 5MB limit" });
            }
            try
            {
                var imageUrl = await _uploadService.UploadImageAsync(file);
                return Ok(new { imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading image: " + ex.Message });
            }
        }

        [HttpGet("admin/blog/api/list")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<IEnumerable<BlogPostDTO>>> GetBlogsAdmin()
        {
            var blogs = await _blogService.GetBlogsAdminAsync();
            return Ok(blogs);
        }

        [HttpGet("admin/blog/api/detail/{id:int}")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<BlogPostDTO>> GetBlogAdmin(int id)
        {
            var blog = await _blogService.GetBlogAdminAsync(id);
            if (blog == null)
            {
                return NotFound(new { message = "Blog not found" });
            }
            return Ok(blog);
        }

        [HttpPost("admin/blog/create")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<BlogPosts>> CreateBlog([FromBody] CreateBlogDTO createDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var blog = await _blogService.CreateBlogAsync(createDTO, currentUserId);
                return Ok(new { message = "Blog created successfully", uid = blog.Uid });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("admin/blog/api/update/{id:int}")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<IActionResult> UpdateBlog(int id, [FromBody] CreateBlogDTO updateDTO)
        {
            try
            {
                var blog = await _blogService.UpdateBlogAsync(id, updateDTO);
                if (blog == null)
                {
                    return NotFound(new { message = "Blog not found" });
                }
                return Ok(new { message = "Blog updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("admin/blog/api/delete/{id:int}")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var success = await _blogService.DeleteBlogAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Blog not found" });
            }
            return Ok(new { message = "Blog deleted successfully" });
        }

        // === BLOG CATEGORIES API ===
        [HttpGet("api/BlogCategories")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<IEnumerable<BlogCategories>>> GetBlogCategories()
        {
            var categories = await _blogService.GetCategoriesAdminAsync();
            return Ok(categories);
        }

        [HttpGet("api/BlogCategories/{id:int}")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<BlogCategories>> GetBlogCategory(int id)
        {
            var category = await _blogService.GetCategoryAdminAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }
            return Ok(category);
        }

        [HttpPost("api/BlogCategories")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<BlogCategories>> CreateBlogCategory(CreateBlogCategoryDTO createDTO)
        {
            var category = await _blogService.CreateCategoryAsync(createDTO);
            return CreatedAtAction(nameof(GetBlogCategory), new { id = category.Uid }, category);
        }

        [HttpPut("api/BlogCategories/{id:int}")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<IActionResult> UpdateBlogCategory(int id, CreateBlogCategoryDTO updateDTO)
        {
            var category = await _blogService.UpdateCategoryAsync(id, updateDTO);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }
            return Ok(new { message = "Category updated successfully" });
        }

        [HttpDelete("api/BlogCategories/{id:int}")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<IActionResult> DeleteBlogCategory(int id)
        {
            try
            {
                var success = await _blogService.DeleteCategoryAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Category not found" });
                }
                return Ok(new { message = "Category deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("admin/blog/api/categories")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<IEnumerable<BlogCategories>>> GetCategoriesAdmin()
        {
            var categories = await _blogService.GetCategoriesAdminAsync();
            return Ok(categories);
        }

        [HttpGet("admin/blog/api/authors")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetBlogAuthors()
        {
            var authors = await _blogService.GetBlogAuthorsAsync();
            return Ok(authors);
        }

        [HttpGet("admin/blog/api/current-user")]
        [Authorize(Policy = "NoCustomer")]
        public async Task<ActionResult<UserDTO>> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            var user = await _blogService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }
    }
}