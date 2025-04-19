namespace shetuan.Models
{
    public class Admin
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Role { get; set; } = "admin";
        public DateTime CreatedAt { get; set; }
    }
}