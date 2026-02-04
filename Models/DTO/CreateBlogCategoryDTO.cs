using System.ComponentModel.DataAnnotations;

namespace Public_Transport.Models.DTO
{
    public class CreateBlogCategoryDTO
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 200 characters")]
        public string Name { get; set; }
    }
}
