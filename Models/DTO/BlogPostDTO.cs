namespace Public_Transport.Models.DTO
{
    public class BlogPostDTO
    {
        public int Uid { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int AuthorUid { get; set; }
        public string AuthorName { get; set; }
        public int CategoryUid { get; set; }
        public string CategoryName { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
