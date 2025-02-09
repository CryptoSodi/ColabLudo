namespace SharedCode.Constants
{
    public class UserInfo
    {
        private static UserInfo _instance;
        private static readonly object _lock = new object();
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string PictureUrl { get; set; }
        public string Number { get; set; }
        public string Location { get; set; }
        public float Coins { get; set; }
        public float PlayerCryptoCoins { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string RegionName { get; set; }
        public string City { get; set; }
        public float Lat { get; set; }
        public float Lon { get; set; }

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
            Preferences.Set(nameof(Number), instance.Number);
            Preferences.Set(nameof(Location), instance.Location);
            Preferences.Set(nameof(Coins), instance.Coins);
            
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
            instance.Id = Preferences.Get(nameof(Id), -1);
            instance.Email = Preferences.Get(nameof(Email), string.Empty);
            instance.Name = Preferences.Get(nameof(Name), string.Empty);
            instance.PictureUrl = Preferences.Get(nameof(PictureUrl), string.Empty);
            instance.Number = Preferences.Get(nameof(Number), "###########");
            instance.Location = Preferences.Get(nameof(Location), "Global");
            instance.Coins = Preferences.Get(nameof(Coins), (float)0.0);
        }
    }
}