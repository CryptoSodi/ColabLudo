using System.ComponentModel.DataAnnotations;

namespace LudoServer.Data.AdminPanel
{
    public class UserDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Role { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
