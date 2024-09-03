using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient
{
    public class UserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool VerifiedEmail { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string PictureUrl { get; set; }
    }
}
