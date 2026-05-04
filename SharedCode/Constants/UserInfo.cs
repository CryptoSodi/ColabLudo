using System.Security.Policy;
using System.Text.Json;

namespace SharedCode.Constants
{
    public class UserInfo
    {
        private static UserInfo? _instance;
        private static readonly object _lock = new object();
        public PlayerInfo player;
        
        public string? AddressQRBlob { get; set; }

        const string BaseUrl = "https://quickchart.io/qr";
        // You can tweak these hex colors and size as you like:
        const string lightColor = "4031af";
        const string darkColor = "ededed";

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

        public ImageSource ProfileImageSource { get; set; }

        // Method to save state
        static bool saving = false;
        public static void SaveState()
        {
            var instance = Instance;
            if (saving) return;
            saving = true;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                // Optionally add for enum as string:
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                // Serialize the whole player info object
                string playerJson = JsonSerializer.Serialize(instance.player, options);

                // Store in preferences
                Preferences.Set("UserProfile", playerJson);
                Preferences.Set("AuthToken", instance.player.AuthToken);

                if (instance.ProfileImageSource == null)
                {
                    // Force decode early                    
                    instance.ProfileImageSource = ImageSource.FromUri(new Uri(instance.player.PictureUrl));
                }

                String QrUrl = $"{BaseUrl}"
                  + $"?text={UserInfo.Instance.player.Wallet?.WalletAddress}"
                  + $"&light={lightColor}"
                  + $"&dark={darkColor}"
                  + $"&size=200";
                instance.AddressQRBlob = DownloadImageAsBase64Async(QrUrl).GetAwaiter().GetResult();
                Preferences.Set(nameof(instance.AddressQRBlob), instance.AddressQRBlob);

                Preferences.Set("IsUserLoggedIn", true);
            }
            catch (Exception ex)
            {
                // Log or handle error
                System.Diagnostics.Debug.WriteLine($"SaveStateAsync error: {ex.Message}");
            }
            finally
            {
                saving = false;
            }
        }
        public static void Logout()
        {
            var instance = Instance;
            Preferences.Clear();
            instance.player = new PlayerInfo();
            instance.player.Wallet = new PlayerWallet();
            instance.AddressQRBlob = string.Empty;
        }
        // Method to load state
        public static void LoadState()
        {
            var instance = Instance;
            var options = new JsonSerializerOptions{PropertyNameCaseInsensitive = true};
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            try
            {
                // Try to get the full PlayerInfo object from Preferences
                string playerJson = Preferences.Get("UserProfile", "");
                if (!string.IsNullOrEmpty(playerJson))
                {
                    instance.player = JsonSerializer.Deserialize<PlayerInfo>(playerJson, options) ?? new PlayerInfo();
                    // Assign to cached property
                    UserInfo.Instance.ProfileImageSource = ImageSource.FromUri(new Uri(instance.player.PictureUrl));

                    instance.player.PhoneNumber = instance.player.PhoneNumber==null || instance.player.PhoneNumber == "" ? "###########" : instance.player.PhoneNumber;
                    instance.player.CountryCallingCode = instance.player.CountryCallingCode == "" ? "###" : instance.player.CountryCallingCode;
                    instance.player.Country = instance.player.Country == "" ? "###########" : instance.player.Country;
                    instance.player.City = instance.player.City == "" ? "###########" : instance.player.City;
                    instance.AddressQRBlob = Preferences.Get(nameof(instance.AddressQRBlob), string.Empty);
                }
                else
                {
                    instance.player = new PlayerInfo();
                }
            }
            catch (Exception ex)
            {
                // Fallback to a new player object and optionally log the error                
                System.Diagnostics.Debug.WriteLine($"LoadState error: {ex.Message}");
            }
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
