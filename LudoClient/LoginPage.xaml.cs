using Acr.UserDialogs;
using LudoClient.Constants;
using LudoClient.Services;
using SharedCode.Constants;
using System.Text.Json;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {
        string city = "none";
        string countryCode = "none";
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
            UserInfo.Instance.Id = result["id"].GetInt32();
            UserInfo.Instance.GoogleId = result?["googleId"].GetString();
            UserInfo.Instance.Name = result?["name"].GetString();
            UserInfo.Instance.Email = result?["email"].GetString();
            UserInfo.Instance.PictureUrl = result?["pictureUrl"].GetString();

            UserInfo.Instance.PictureUrlBlob = UserInfo.DownloadImageAsBase64Async(UserInfo.Instance.PictureUrl).GetAwaiter().GetResult();
            
            UserInfo.Instance.PhoneNumber = result?["phoneNumber"].GetString();
            UserInfo.Instance.CountryCode = result?["countryCode"].GetString();
            UserInfo.Instance.City = result?["city"].GetString();

            UserInfo.Instance.GamesPlayed = result["gamesPlayed"].GetInt32();
            UserInfo.Instance.GamesWon = result["gamesWon"].GetInt32();
            UserInfo.Instance.GamesLost = result["gamesLost"].GetInt32();

            UserInfo.Instance.BestWin = result["bestWin"].GetDecimal();
            UserInfo.Instance.TotalWin = result["totalWin"].GetDecimal();
            UserInfo.Instance.TotalLost = result["totalLost"].GetDecimal();
        }
       
        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
            if (_isLoggingIn)
                return;
            _isLoggingIn = true;
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
#if WINDOWS
            await performLoginAsync("Guest1");
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
            }
            catch (Exception ex)
            {
                await DisplayAlert("Google Sign-In FAILED", "Sign-in returned no token.", "OK");            
                return;
            }
            if (authService != null)
            {
                string idToken = await authService.SignInAsync();
                // Successfully signed in
                
                await performLoginAsync(idToken);
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
        private async Task performLoginAsync(String idToken)
        {
            try
            {
                if (GlobalConstants.online)
                {
                    string url = $"api/GoogleAuthentication?idToken={idToken}&city={city}&countryCode={countryCode}";

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
                    string message = result?["message"].GetString();

                    if (message == "Player login successfully." || message == "Player updated successfully." || message == "Player created successfully." || message == "Attach Phone.")
                    {
                        SetupInstance(result);

                        await UserInfo.SaveState();
                        UserInfo.LoadState();

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
                        root.TryGetProperty("city", out JsonElement cityElement) && root.TryGetProperty("countryCode", out JsonElement countryCodeElement))
                    {
                        city = cityElement.GetString();
                        countryCode = countryCodeElement.GetString() == "PK"?"+92":"+91";
                    }
                    else
                    {
                        // Handle cases where 'status' is not 'success' or 'city' property is missing
                        countryCode = "null";
                        city = "null";
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Handle HTTP request errors
                    countryCode = ex.Message;
                    city = ex.Message;
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing errors
                    countryCode = ex.Message;
                    city = ex.Message;
                }
            }
            catch (Exception ex)
            {
                countryCode = ex.Message;
                city = ex.Message;
                Console.WriteLine($"Error: Country {ex.Message}");
            }
        }
    }
}