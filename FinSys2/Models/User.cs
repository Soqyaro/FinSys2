namespace FinSys2.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public string AvatarPath { get; set; } = "/images/BaseUser.jpg";
    }
}