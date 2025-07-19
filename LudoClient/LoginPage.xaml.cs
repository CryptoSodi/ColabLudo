using Acr.UserDialogs;
using LudoClient.Constants;
using LudoClient.Services;
using Microsoft.AspNetCore.SignalR.Client;
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

            GlobalConstants.MatchMaker.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GlobalConstants.MatchMaker.Connected))
                    MainThread.BeginInvokeOnMainThread(() =>
                        UpdateButtons(GlobalConstants.MatchMaker.Connected));
            };
        }

        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
            if (!GlobalConstants.MatchMaker.Connected)
            {
                await DisplayAlert("Google Sign-In Failed", "Not connected to server. Reload the app if the issue presists.", "OK");
                return;
            }
            if (_isLoggingIn)
                return;
            _isLoggingIn = true;
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
#if WINDOWS
            await performLoginAsync("Guest2");
            _isLoggingIn = false;
            return;
#endif
#if ANDROID
            // Show loading indicator on the main thread
            MainThread.BeginInvokeOnMainThread(() => {
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
                try
                {
                    string idToken = await authService.SignInAsync();
                    await performLoginAsync(idToken);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Google Sign-In Failed.", ex.Message, "OK");
                }
                // Successfully signed in
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
                if (GlobalConstants.MatchMaker.Connected)
                {
                    UserInfo.Instance.player = await GlobalConstants.MatchMaker._hubConnection.InvokeAsync<PlayerInfo>("GoogleAuthentication", idToken, city, countryCode).ConfigureAwait(false);

                    if (UserInfo.Instance.player != null)
                    {
                        UserInfo.SaveState();
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Application.Current.MainPage = new AppShell();
                        });
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            DisplayAlert("Error", $"An error occurred: Player not created", "OK");
                        });
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
                        countryCode = countryCodeElement.GetString() == "PK" ? "+92" : "+91";
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
        private void UpdateButtons(bool isConnected)
        {
            GoogleLoginPanel.Source = isConnected ? "google_login.png" : "google_login_gray.png";
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateButtons(GlobalConstants.MatchMaker.Connected);
        }
    }
}