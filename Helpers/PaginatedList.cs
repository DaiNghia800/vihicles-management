using Microsoft.EntityFrameworkCore;

namespace Public_Transport.Helpers
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        // Kiểm tra xem có trang trước/sau không
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        //tạo danh sách phân trang từ IQueryable
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync(); // Đếm tổng số
            var items = await source.Skip((pageIndex - 1) * pageSize) // Bỏ qua các trang trước
                                    .Take(pageSize) // Chỉ lấy số lượng cho trang này
                                    .ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
