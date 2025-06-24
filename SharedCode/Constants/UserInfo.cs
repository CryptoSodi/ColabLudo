namespace SharedCode.Constants
{
    public class UserInfo
    {
        private static UserInfo? _instance;
        private static readonly object _lock = new object();
        public PlayerInfo player;
        public string? PictureUrlBlob { get; set; }
        public string? WalletAddress { get; set; }
        public decimal AvailableBalance { get; set; }
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
        public static async void SaveState()
        {
            var instance = Instance;
            Preferences.Set(nameof(player.PlayerId), instance.player.PlayerId);
            Preferences.Set(nameof(player.GoogleId), instance.player.GoogleId);
            Preferences.Set(nameof(player.Email), instance.player.Email);
            Preferences.Set(nameof(player.Name), instance.player.Name);
            Preferences.Set(nameof(player.PictureUrl), instance.player.PictureUrl);
            instance.PictureUrlBlob = await DownloadImageAsBase64Async(instance.player.PictureUrl);
            Preferences.Set(nameof(PictureUrlBlob), instance.PictureUrlBlob);
            Preferences.Set(nameof(player.PhoneNumber), instance.player.PhoneNumber);
            Preferences.Set(nameof(player.CountryCode), instance.player.CountryCode);
            Preferences.Set(nameof(player.City), instance.player.City);
            Preferences.Set(nameof(player.GamesPlayed), instance.player.GamesPlayed);
            Preferences.Set(nameof(player.GamesWon), instance.player.GamesWon);
            Preferences.Set(nameof(player.GamesLost), instance.player.GamesLost);
            Preferences.Set(nameof(player.BestWin), instance.player.BestWin + "");
            Preferences.Set(nameof(player.TotalLost), instance.player.TotalLost + "");
            Preferences.Set(nameof(player.TotalWin), instance.player.TotalWin + "");
            Preferences.Set(nameof(player.IsActive), instance.player.IsActive);
            Preferences.Set(nameof(player.Score), instance.player.Score);
            Preferences.Set(nameof(player.AuthToken), instance.player.AuthToken);
            
            Preferences.Set(nameof(WalletAddress), instance.player.Wallets.FirstOrDefault()?.WalletAddress);
            
            decimal? balance = instance.player.Wallets.FirstOrDefault()?.AvailableBalance;
            if (balance.HasValue)
                Preferences.Set(nameof(AvailableBalance), (double)balance.Value);

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
            instance.player = new PlayerInfo(); // Ensure player is initialized
            instance.player.PlayerId = Preferences.Get(nameof(player.PlayerId), -1);
            instance.player.GoogleId = Preferences.Get(nameof(player.GoogleId), string.Empty);
            instance.player.Email = Preferences.Get(nameof(player.Email), string.Empty);
            instance.player.Name = Preferences.Get(nameof(player.Name), string.Empty);
            instance.player.PictureUrl = Preferences.Get(nameof(player.PictureUrl), string.Empty);
            instance.PictureUrlBlob = Preferences.Get(nameof(PictureUrlBlob), string.Empty);            
            instance.player.PhoneNumber = Preferences.Get(nameof(player.PhoneNumber), "###########");
            instance.player.CountryCode = Preferences.Get(nameof(player.CountryCode), "###");
            instance.player.City = Preferences.Get(nameof(player.City), "###########");
            instance.player.GamesPlayed = Preferences.Get(nameof(player.GamesPlayed), 0);
            instance.player.GamesWon = Preferences.Get(nameof(player.GamesWon), 0);
            instance.player.GamesLost = Preferences.Get(nameof(player.GamesLost), 0);
            instance.player.BestWin = decimal.Parse(Preferences.Get(nameof(player.BestWin), "0"));
            instance.player.TotalLost = decimal.Parse(Preferences.Get(nameof(player.TotalLost), "0"));
            instance.player.TotalWin = decimal.Parse(Preferences.Get(nameof(player.TotalWin), "0"));
            instance.player.IsActive = Preferences.Get(nameof(player.IsActive), true);
            instance.player.Score = Preferences.Get(nameof(player.Score), 0);
            instance.player.AuthToken =  Preferences.Get(nameof(player.AuthToken), "");
            double savedBalance = Preferences.Get(nameof(AvailableBalance), 0.0);

            instance.player.Wallets = new List<PlayerWallet>{
                    new PlayerWallet{
                        PlayerId = instance.player.PlayerId,
                        AddressType = "SOL",
                        WalletAddress = Preferences.Get(nameof(WalletAddress), ""),
                        AvailableBalance = (decimal)savedBalance
                    }};
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