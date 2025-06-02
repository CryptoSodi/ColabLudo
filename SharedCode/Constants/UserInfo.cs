namespace SharedCode.Constants
{
    public class UserInfo
    {
        private static UserInfo _instance;
        private static readonly object _lock = new object();
        
        public int Id { get; set; }
        public string GoogleId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PictureUrl { get; set; }
        public string? PictureUrlBlob { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CountryCode { get; set; }
        public string? City { get; set; }
        public string? Otp { get; set; }
        public bool IsActive { get; set; } = true;
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public decimal BestWin { get; set; }
        public decimal TotalLost { get; set; }
        public decimal TotalWin { get; set; }
        public int Score { get; set; } = 0;

        public double? LudoCoins { get; set; } = 0;
        public string CryptoAddress { get; set; }

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
        public static async Task SaveState()
        {
            var instance = Instance;
            Preferences.Set(nameof(Id), instance.Id); 
            Preferences.Set(nameof(GoogleId), instance.GoogleId);
            Preferences.Set(nameof(Email), instance.Email);
            Preferences.Set(nameof(Name), instance.Name);
            Preferences.Set(nameof(PictureUrl), instance.PictureUrl);
            Preferences.Set(nameof(PictureUrlBlob), instance.PictureUrlBlob);            
            Preferences.Set(nameof(PhoneNumber), instance.PhoneNumber);
            Preferences.Set(nameof(CountryCode), instance.CountryCode);
            Preferences.Set(nameof(City), instance.City);
            Preferences.Set(nameof(GamesPlayed), instance.GamesPlayed);
            Preferences.Set(nameof(GamesWon), instance.GamesWon);
            Preferences.Set(nameof(GamesLost), instance.GamesLost);
            Preferences.Set(nameof(BestWin), instance.BestWin + "");
            Preferences.Set(nameof(TotalLost), instance.TotalLost + "");
            Preferences.Set(nameof(TotalWin), instance.TotalWin + "");
            Preferences.Set(nameof(IsActive), instance.IsActive);
            Preferences.Set(nameof(Score), instance.Score);

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
            instance.GoogleId = Preferences.Get(nameof(GoogleId), string.Empty);
            instance.Email = Preferences.Get(nameof(Email), string.Empty);
            instance.Name = Preferences.Get(nameof(Name), string.Empty);
            instance.PictureUrl = Preferences.Get(nameof(PictureUrl), string.Empty);
            instance.PictureUrlBlob = Preferences.Get(nameof(PictureUrlBlob), string.Empty);
            instance.PhoneNumber = Preferences.Get(nameof(PhoneNumber), "###########");
            instance.CountryCode = Preferences.Get(nameof(CountryCode), "###");
            instance.City = Preferences.Get(nameof(City), "###########");
            instance.GamesPlayed = Preferences.Get(nameof(GamesPlayed), 0);
            instance.GamesWon = Preferences.Get(nameof(GamesWon), 0);
            instance.GamesLost = Preferences.Get(nameof(GamesLost), 0);
            instance.BestWin = decimal.Parse(Preferences.Get(nameof(BestWin), "0"));
            instance.TotalLost = decimal.Parse(Preferences.Get(nameof(TotalLost), "0"));
            instance.TotalWin = decimal.Parse(Preferences.Get(nameof(TotalWin), "0"));
            instance.IsActive = Preferences.Get(nameof(IsActive), true);
            instance.Score = Preferences.Get(nameof(Score), 0);
        }
        public static async Task<string> DownloadImageAsBase64Async(string imageUrl)
        {
            byte[] imageBytes = await new HttpClient().GetByteArrayAsync(imageUrl).ConfigureAwait(false);            
            return Convert.ToBase64String(imageBytes);
        }
        public static ImageSource ConvertBase64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            return ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }
    }
}