namespace shetuan.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "user"; // 默认角色为 "user"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}