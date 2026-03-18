using System.ComponentModel.DataAnnotations;

namespace LudoServer.Data.AdminPanel
{
    public class UserLoginDto
    {
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }
    }
}
