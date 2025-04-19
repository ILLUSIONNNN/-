namespace shetuan.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string RecipientEmail { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
