namespace Public_Transport.Models.Entities
{
    public class BlogPosts
    {
        public int Uid { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Users Users { get; set; }
        public int AuthorUid { get; set; }
        public BlogCategories Category { get; set; }
        public int CategoryUid { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
