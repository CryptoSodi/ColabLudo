using System.Threading.Tasks;

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
        public string PictureBlob { get; set; }
        public string PhoneNumber { get; set; }
        public float Coins { get; set; }
        public float PlayerCryptoCoins { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string RegionName { get; set; }
        public string City { get; set; }
        public float Lat { get; set; }
        public float Lon { get; set; }
        public string Address { get; set; }
        public double SolBalance { get; set; }

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
            Preferences.Set(nameof(Email), instance.Email);
            Preferences.Set(nameof(Name), instance.Name);
            Preferences.Set(nameof(PictureUrl), instance.PictureUrl);
            Preferences.Set(nameof(PhoneNumber), instance.PhoneNumber);
            Preferences.Set(nameof(Coins), instance.Coins);
            Preferences.Set(nameof(PlayerCryptoCoins), instance.PlayerCryptoCoins);
            Preferences.Set(nameof(Country), instance.Country);
            Preferences.Set(nameof(CountryCode), instance.CountryCode);
            Preferences.Set(nameof(RegionName), instance.RegionName);
            Preferences.Set(nameof(City), instance.City);
            Preferences.Set(nameof(Lat), instance.Lat);
            Preferences.Set(nameof(Lon), instance.Lon);
            await DownloadImageAsBase64Async(instance.PictureUrl);
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
            instance.PictureBlob = Preferences.Get(nameof(PictureBlob), string.Empty);
            instance.PhoneNumber = Preferences.Get(nameof(PhoneNumber), "###########");
            instance.Coins = Preferences.Get(nameof(Coins), (float)0.0);
            instance.PlayerCryptoCoins = Preferences.Get(nameof(PlayerCryptoCoins), (float)0.0);
            instance.Country = Preferences.Get(nameof(Country), "###########");
            instance.CountryCode = Preferences.Get(nameof(CountryCode), "###########");
            instance.RegionName = Preferences.Get(nameof(RegionName), "###########");
            instance.City = Preferences.Get(nameof(City), "###########");
            instance.Lat = Preferences.Get(nameof(Lat), (float)0.0);
            instance.Lon = Preferences.Get(nameof(Lon), (float)0.0);
        }
        public static async Task DownloadImageAsBase64Async(string imageUrl)
        {
            using HttpClient client = new HttpClient();
            byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
            Instance.PictureBlob = Convert.ToBase64String(imageBytes);
            Preferences.Set(nameof(PictureBlob), Convert.ToBase64String(imageBytes));
        }
        public static ImageSource ConvertBase64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            return ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }
    }
}