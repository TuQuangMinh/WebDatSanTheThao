namespace web.Models
{
    public class SanImage
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public int SanId { get; set; }
        public San? San { get; set; }
    }
}
