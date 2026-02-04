using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Public_Transport.Models.DTO;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "NoPassenger")]
    public class BlogCategoriesController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public BlogCategoriesController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        // GET: api/BlogCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogCategories>>> GetCategories()
        {
            var categories = await _blogService.GetCategoriesAdminAsync();
            return Ok(categories);
        }

        // GET: api/BlogCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BlogCategories>> GetCategory(int id)
        {
            var category = await _blogService.GetCategoryAdminAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }
            return Ok(category);
        }

        // POST: api/BlogCategories
        [HttpPost]
        public async Task<ActionResult<BlogCategories>> CreateCategory([FromBody] CreateBlogCategoryDTO createDTO)
        {
            if (createDTO == null || string.IsNullOrWhiteSpace(createDTO.Name))
            {
                return BadRequest(new { message = "Category name is required" });
            }

            try
            {
                var category = await _blogService.CreateCategoryAsync(createDTO);
                return CreatedAtAction(nameof(GetCategory), new { id = category.Uid }, category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating category", error = ex.Message });
            }
        }

        // PUT: api/BlogCategories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateBlogCategoryDTO updateDTO)
        {
            if (updateDTO == null || string.IsNullOrWhiteSpace(updateDTO.Name))
            {
                return BadRequest(new { message = "Category name is required" });
            }

            try
            {
                var category = await _blogService.UpdateCategoryAsync(id, updateDTO);
                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }
                return Ok(new { message = "Category updated successfully", category });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating category", error = ex.Message });
            }
        }

        // DELETE: api/BlogCategories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}