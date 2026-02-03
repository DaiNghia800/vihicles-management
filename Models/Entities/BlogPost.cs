using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System; // Thêm cái này để dùng DateTime

namespace Public_Transport.Models.Entities
{
    public class BlogPost
    {
        [Key]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Tác giả là bắt buộc")]
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public virtual Users Author { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual BlogCategory Category { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        public string Content { get; set; }

        [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự")]
        public string? ThumbnailUrl { get; set; }

        public int ViewsCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}