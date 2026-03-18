using System.Security.Cryptography;
using System.Text;

namespace LudoServer.Services
{
    public class PasswordService
    {
        private readonly string _secretKey;

        public PasswordService(string secretKey)
        {
            _secretKey = secretKey;
        }

        public string HashPassword(string password)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            }
        }
    }

}
