namespace LudoClient.Constants
{
    public class UserInfo
    {
        private static UserInfo _instance;
        private static readonly object _lock = new object();
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
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
            Preferences.Set(nameof(Name), instance.Name);
            Preferences.Set(nameof(PictureUrl), instance.PictureUrl);
            Preferences.Set("IsUserLoggedIn", true);
        }
        public static void Logout()
        {
            Preferences.Clear();
        }
        // Method to load state
        public static void LoadState()
        {
            var instance = Instance;
            instance.Id = Preferences.Get(nameof(Id), 0);
            instance.Email = Preferences.Get(nameof(Email), string.Empty);
            instance.Name = Preferences.Get(nameof(Name), string.Empty);
            instance.PictureUrl = Preferences.Get(nameof(PictureUrl), string.Empty);
        }
    }
}