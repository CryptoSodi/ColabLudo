using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient
{
    public class UserInfo
    {
        private static UserInfo _instance;
        private static readonly object _lock = new object();
        public string Id { get; set; }
        public string Email { get; set; }
        public bool VerifiedEmail { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string PictureUrl { get; set; }
        private UserInfo() { }
        public static UserInfo Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UserInfo();
                        }
                    }
                }
                return _instance;
            }
        }
        // Method to save state
        public static void SaveState()
        {
            var instance = Instance;
            Preferences.Set(nameof(Id), instance.Id);
            Preferences.Set(nameof(Email), instance.Email);
            Preferences.Set(nameof(VerifiedEmail), instance.VerifiedEmail);
            Preferences.Set(nameof(Name), instance.Name);
            Preferences.Set(nameof(GivenName), instance.GivenName);
            Preferences.Set(nameof(FamilyName), instance.FamilyName);
            Preferences.Set(nameof(PictureUrl), instance.PictureUrl);
        }
        // Method to load state
        public static void LoadState()
        {
            var instance = Instance;
            instance.Id = Preferences.Get(nameof(Id), string.Empty);
            instance.Email = Preferences.Get(nameof(Email), string.Empty);
            instance.VerifiedEmail = Preferences.Get(nameof(VerifiedEmail), false);
            instance.Name = Preferences.Get(nameof(Name), string.Empty);
            instance.GivenName = Preferences.Get(nameof(GivenName), string.Empty);
            instance.FamilyName = Preferences.Get(nameof(FamilyName), string.Empty);
            instance.PictureUrl = Preferences.Get(nameof(PictureUrl), string.Empty);
        }
    }
}