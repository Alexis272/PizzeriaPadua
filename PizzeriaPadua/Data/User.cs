namespace PizzeriaPadua.Core.Data
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; } = "";
        public string? ProfilePicture { get; set; }
        public string CustomerType { get; set; } = "General";
        public string? Rfc { get; set; }
        public bool IsBlocked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
