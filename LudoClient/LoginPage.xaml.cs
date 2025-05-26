using Acr.UserDialogs;
using LudoClient.Services;
using SharedCode.Constants;
using System.Text.Json;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {
        string city = "none";
        private bool _isLoggingIn = false;
        public LoginPage()
        {
            InitializeComponent();
         
            Task.Run(() =>
            {
                GetCountryByIpAsync();
            });

            string build = VersionTracking.CurrentBuild;
            VersionText.Text = "Version : " + build;
        }
        void SetupInstance(Dictionary<string, JsonElement>? result)
        {
            int playerId = result["playerId"].GetInt32();
            string PlayerName = result["playerName"].GetString();
            string Email = result["email"].GetString();
            string PlayerPicture = result["playerPicture"].GetString();
            string phoneNumber = result["phoneNumber"].GetString();
            double PlayerLudoCoins = result["playerLudoCoins"].GetDouble();
            double PlayerCryptoCoins = result["playerCryptoCoins"].GetDouble();
     
            UserInfo.Instance.Id = playerId;
            UserInfo.Instance.PhoneNumber = phoneNumber;
            UserInfo.Instance.City = city;

            UserInfo.Instance.Name = PlayerName;
            UserInfo.Instance.Email = Email;
            UserInfo.Instance.PictureUrl = PlayerPicture;
            UserInfo.Instance.Coins = (float)PlayerLudoCoins;
            UserInfo.Instance.PlayerCryptoCoins = (float)PlayerCryptoCoins;
        }
        private async void Guest_Login_Clicked(object sender, EventArgs e)
        {
            if (_isLoggingIn)
                return;
            _isLoggingIn = true;

            try
            {
#if ANDROID
                UserDialogs.Instance.ShowLoading("Logging in as Guest.", MaskType.Black);                
#endif
                var deviceId = this.Handler.MauiContext.Services.GetService<IDeviceIdentifierService>()?.GetDeviceId();

                UserInfo.Instance.Id = 19;
                UserInfo.Instance.Email = deviceId + "@LudoNFT.com";
                UserInfo.Instance.Name = deviceId + "";
                UserInfo.Instance.PictureUrl = "https://ludoNFT.online/player.png";

                //Save the user's login state
                await UserInfo.SaveState();
                UserInfo.LoadState();
                Application.Current.MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await DisplayAlert("Error", "Failed to sign in: " + ex.Message, "OK");
            }
#if ANDROID
            UserDialogs.Instance.HideLoading();
#endif
            _isLoggingIn = false;
        }
        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
            if (_isLoggingIn)
                return;
            _isLoggingIn = true;
#if WINDOWS
            await performLoginAsync("-1","Sodi", "Sodi@gmail.com","https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj");
            _isLoggingIn = false;
            return;
#endif
#if ANDROID
            // Show loading indicator on the main thread
            MainThread.BeginInvokeOnMainThread(() =>{
                UserDialogs.Instance.ShowLoading("Logging in with Google.", MaskType.Black);
            });
#endif
            IGoogleAuthService authService = null;
            try
            {
                authService = DependencyService.Get<IGoogleAuthService>();
                var idToken = await authService.SignInAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Google Sign-In FAILED", "Sign-in returned no token.", "OK");            
                return;
            }
            if (authService != null)
            {   
                // Successfully signed in
                await performLoginAsync((UserInfo.Instance.Id != null ? UserInfo.Instance.Id.ToString() : "-1"),authService.UserName,authService.UserEmail,authService.UserPhotoUrl);
            }
            else
            {
                await DisplayAlert("Google Sign-In", "Sign-in returned no token.", "OK");                
            }
#if ANDROID
            UserDialogs.Instance.HideLoading();
#endif
            _isLoggingIn = false;
        }
        private async Task performLoginAsync(String Id, String Name,String Email,String PicURL)
        {
            try
            {
                if (GlobalConstants.online)
                {
                    string url = $"api/GoogleAuthentication?name={Name}&email={Email}&pictureUrl={PicURL}&playerId={Id}";

                    Dictionary<string, JsonElement>? result = null;
                    
                    try
                    {
                        HttpResponseMessage response = GlobalConstants.httpClient.GetAsync(url).GetAwaiter().GetResult();
                        string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Server Error", $"An error occurred: {ex.Message}", "OK");
                    }

                    SetupInstance(result);
                    string message = result?["message"].GetString();

                    if (message == "Player login successfully." || message == "Player updated successfully." || message == "Player created successfully." || message == "Attach Phone.")
                    {

                        UserInfo.Instance.Name = Name;
                        UserInfo.Instance.Email = Email;
                        UserInfo.Instance.PictureUrl = PicURL;
                        //Save the user's login state
                        await UserInfo.SaveState();
                        UserInfo.LoadState();
                        //Hide Loader
                        Application.Current.MainPage = new AppShell();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
        public async void GetCountryByIpAsync()
        {
            try
            {
                using HttpClient client = new HttpClient();
                //{"status":"success","country":"Pakistan","countryCode":"PK","region":"PB","regionName":"Punjab","city":"Lahore","zip":"54020","lat":31.558,"lon":74.3587,"timezone":"Asia/Karachi","isp":"Cloudflare, Inc.","org":"Cloudflare WARP","as":"AS13335 Cloudflare, Inc.","query":"104.28.212.126"}
                //string url = "http://ip-api.com/json/?token=0cf62c767a1ab9";
                string url = "http://ip-api.com/json/?token=0cf62c767a1ab9&fields=status,countryCode,city";
                try
                {
                    var response = await client.GetStringAsync(url);
                    using JsonDocument doc = JsonDocument.Parse(response);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("status", out JsonElement statusElement) &&
                        statusElement.GetString() == "success" &&
                        root.TryGetProperty("city", out JsonElement cityElement))
                    {
                        city = cityElement.GetString();
                    }
                    else
                    {
                        // Handle cases where 'status' is not 'success' or 'city' property is missing
                        city = "null";
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Handle HTTP request errors
                    city = ex.Message;
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing errors
                    city = ex.Message;
                }
            }
            catch (Exception ex)
            {
                city = ex.Message;
                Console.WriteLine($"Error: Country {ex.Message}");
            }
        }
    }
}