using Public_Transport.Models.DTO;
using Public_Transport.Models.Entities;

namespace Public_Transport.Services.IServices
{
    public interface IBlogService
        {
            // === Public BlogController ===
            Task<object> GetBlogsPublicAsync();
            Task<object> GetBlogDetailPublicAsync(int id);
            Task<object> GetCategoriesPublicAsync();

            // === Admin BlogCategoriesController ===
            Task<IEnumerable<BlogCategories>> GetCategoriesAdminAsync();
            Task<BlogCategories> GetCategoryAdminAsync(int id);
            Task<BlogCategories> CreateCategoryAsync(CreateBlogCategoryDTO DTO);
            Task<BlogCategories> UpdateCategoryAsync(int id, CreateBlogCategoryDTO DTO);
            Task<bool> DeleteCategoryAsync(int id); // Trả về true nếu thành công, false/exception nếu thất bại

            // === Admin BlogAdminController ===
            Task<IEnumerable<BlogPostDTO>> GetBlogsAdminAsync();
            Task<BlogPostDTO> GetBlogAdminAsync(int id);
            Task<BlogPosts> CreateBlogAsync(CreateBlogDTO DTO, int authorId);
            Task<BlogPosts> UpdateBlogAsync(int id, CreateBlogDTO DTO);
            Task<bool> DeleteBlogAsync(int id);
            Task<IEnumerable<UserDTO>> GetBlogAuthorsAsync();
            Task<UserDTO> GetCurrentUserAsync(int id);
        }
}

