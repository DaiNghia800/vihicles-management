using Microsoft.EntityFrameworkCore;
using Public_Transport.Helpers;
using Public_Transport.Models.DTO;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Services
{
    public class BlogService : IBlogService
    {
        private readonly ApplicationDbContext _context;

        public BlogService(ApplicationDbContext context)
        {
            _context = context;
        }

        // === Public BlogController ===
        public async Task<object> GetBlogsPublicAsync()
        {
            return await _context.BlogPosts
                .Include(b => b.Users)
                .Include(b => b.Category)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    uid = b.Uid,
                    title = b.Title,
                    content = b.Content,
                    authorUid = b.AuthorUid,
                    authorName = b.Users.FullName,
                    categoryUid = b.CategoryUid,
                    categoryName = b.Category.Name,
                    imageUrl = b.ImageUrl,
                    createdAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<object> GetBlogDetailPublicAsync(int id)
        {
            return await _context.BlogPosts
                .Include(b => b.Users)
                .Include(b => b.Category)
                .Where(b => b.Uid == id)
                .Select(b => new
                {
                    uid = b.Uid,
                    title = b.Title,
                    content = b.Content,
                    authorUid = b.AuthorUid,
                    authorName = b.Users.FullName,
                    categoryUid = b.CategoryUid,
                    categoryName = b.Category.Name,
                    imageUrl = b.ImageUrl,
                    createdAt = b.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<object> GetCategoriesPublicAsync()
        {
            return await _context.BlogCategories
                .Select(c => new
                {
                    uid = c.Uid,
                    name = c.Name
                })
                .OrderBy(c => c.name)
                .ToListAsync();
        }

        // === Admin BlogCategoriesController ===
        public async Task<IEnumerable<BlogCategories>> GetCategoriesAdminAsync()
        {
            return await _context.BlogCategories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<BlogCategories> GetCategoryAdminAsync(int id)
        {
            return await _context.BlogCategories.FindAsync(id);
        }

        public async Task<BlogCategories> CreateCategoryAsync(CreateBlogCategoryDTO DTO)
        {
            var category = new BlogCategories
            {
                Name = DTO.Name
            };
            _context.BlogCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<BlogCategories> UpdateCategoryAsync(int id, CreateBlogCategoryDTO DTO)
        {
            var category = await _context.BlogCategories.FindAsync(id);
            if (category == null)
            {
                return null; // Hoặc throw exception
            }
            category.Name = DTO.Name;
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.BlogCategories.FindAsync(id);
            if (category == null)
            {
                return false; // Không tìm thấy
            }

            var hasBlogs = await _context.BlogPosts.AnyAsync(b => b.CategoryUid == id);
            if (hasBlogs)
            {
                // Không thể xóa, ném lỗi
                throw new InvalidOperationException("Cannot delete category that has blogs");
            }

            _context.BlogCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        // === Admin BlogAdminController ===
        public async Task<IEnumerable<BlogPostDTO>> GetBlogsAdminAsync()
        {
            return await _context.BlogPosts
                .Include(b => b.Users)
                .Include(b => b.Category)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BlogPostDTO
                {
                    Uid = b.Uid,
                    Title = b.Title,
                    Content = b.Content,
                    AuthorUid = b.AuthorUid,
                    AuthorName = b.Users.FullName,
                    CategoryUid = b.CategoryUid,
                    CategoryName = b.Category.Name,
                    ImageUrl = b.ImageUrl,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<BlogPostDTO> GetBlogAdminAsync(int id)
        {
            return await _context.BlogPosts
                .Include(b => b.Users)
                .Include(b => b.Category)
                .Where(b => b.Uid == id)
                .Select(b => new BlogPostDTO
                {
                    Uid = b.Uid,
                    Title = b.Title,
                    Content = b.Content,
                    AuthorUid = b.AuthorUid,
                    AuthorName = b.Users.FullName,
                    CategoryUid = b.CategoryUid,
                    CategoryName = b.Category.Name,
                    ImageUrl = b.ImageUrl,
                    CreatedAt = b.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<BlogPosts> CreateBlogAsync(CreateBlogDTO DTO, int authorId)
        {
            // Validate category
            var categoryExists = await _context.BlogCategories.AnyAsync(c => c.Uid == DTO.CategoryUid);
            if (!categoryExists)
            {
                throw new ArgumentException("Invalid category");
            }

            var blog = new BlogPosts
            {
                Title = DTO.Title,
                Content = DTO.Content,
                AuthorUid = authorId, // Gán author đang đăng nhập
                CategoryUid = DTO.CategoryUid,
                ImageUrl = DTO.ImageUrl,
                CreatedAt = DateTime.Now
            };

            _context.BlogPosts.Add(blog);
            await _context.SaveChangesAsync();
            return blog;
        }

        public async Task<BlogPosts> UpdateBlogAsync(int id, CreateBlogDTO DTO)
        {
            var blog = await _context.BlogPosts.FindAsync(id);
            if (blog == null)
            {
                return null; // Hoặc throw
            }

            // Validate category
            var categoryExists = await _context.BlogCategories.AnyAsync(c => c.Uid == DTO.CategoryUid);
            if (!categoryExists)
            {
                throw new ArgumentException("Invalid category");
            }

            blog.Title = DTO.Title;
            blog.Content = DTO.Content;
            blog.CategoryUid = DTO.CategoryUid;
            blog.ImageUrl = DTO.ImageUrl;

            await _context.SaveChangesAsync();
            return blog;
        }

        public async Task<bool> DeleteBlogAsync(int id)
        {
            var blog = await _context.BlogPosts.FindAsync(id);
            if (blog == null)
            {
                return false;
            }

            _context.BlogPosts.Remove(blog);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserDTO>> GetBlogAuthorsAsync()
        {
            // === SỬA LỖI LOGIC Ở ĐÂY ===
            // Thay vì "Admin", hãy dùng hằng số WebConstants.ROLE_ADMIN
            // Hoặc, để khớp với policy "NoCustomer", chúng ta sẽ lấy BẤT KỲ AI không phải là "Customer"
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Deleted == false && u.Role.RoleName != WebConstants.ROLE_CUSTOMER)
                .Select(u => new UserDTO
                {
                    Uid = u.Uid,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToListAsync();
        }

        public async Task<UserDTO> GetCurrentUserAsync(int id)
        {
            return await _context.Users
               .Where(u => u.Uid == id && u.Deleted == false)
               .Select(u => new UserDTO
               {
                   Uid = u.Uid,
                   FullName = u.FullName,
                   Email = u.Email
               })
               .FirstOrDefaultAsync();
        }
    }
}
