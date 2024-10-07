using Java.Net;
using LudoClient.Constants;
using LudoClient.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {

        const string authenticationUrl = "https://xamarin-essentials-auth-sample.azurewebsites.net/mobileauth/";
        private readonly HttpClient _httpClient;
        private string fullPhoneNumber;
        public LoginPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7255/") }; // Replace with your API base URL
        }
        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
            try
            {
                // String AuthToken = "ya29.a0AcM612zzOEp0Jib5dz6rZMcxFj1fuzGZY3E0vgx6ySSaSYsqDMCfHpqD1EfuJHqxleDL1Yg8oprBAGDpfZA6-kE05X44Dlrlwuxx_4al0Drh8r3moeAhnS02pN5MT8QU39FRgNPi_jZOj_nJbpvYyOw4yBWIVplcI1f7aCgYKAT0SARISFQHGX2MiTinthg_X4wC-CNbqU2azmQ0171";
                //  GetUserInfoAsync(AuthToken);
                //   SemanticScreenReader.Announce(CounterBtn.Text);
                OnAuthenticate("Google");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // Handle or log the error
                await DisplayAlert("Error", "Failed to sign in: " + ex.Message, "OK");
            }
        }
        async Task OnAuthenticate(string scheme)
        {
            String AuthToken;
            try
            {
                WebAuthenticatorResult r = null;

                if (scheme.Equals("Apple", StringComparison.Ordinal)
                    && DeviceInfo.Platform == DevicePlatform.iOS
                    && DeviceInfo.Version.Major >= 13)
                {
                    // Make sure to enable Apple Sign In in both the
                    // entitlements and the provisioning profile.
                    var options = new AppleSignInAuthenticator.Options
                    {
                        IncludeEmailScope = true,
                        IncludeFullNameScope = true,
                    };
                    r = await AppleSignInAuthenticator.AuthenticateAsync(options);
                }
                else
                {
                    var authUrl = new Uri(authenticationUrl + scheme);
                    var callbackUrl = new Uri("xamarinessentials://");

                    r = await WebAuthenticator.AuthenticateAsync(authUrl, callbackUrl);
                }

                AuthToken = string.Empty;
                if (r.Properties.TryGetValue("name", out var name) && !string.IsNullOrEmpty(name))
                    AuthToken += $"Name: {name}{Environment.NewLine}";
                if (r.Properties.TryGetValue("email", out var email) && !string.IsNullOrEmpty(email))
                    AuthToken += $"Email: {email}{Environment.NewLine}";
                AuthToken += r?.AccessToken ?? r?.IdToken;
                GetUserInfoAsync(AuthToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Login canceled.");

                AuthToken = string.Empty;
                DisplayAlert("Error", "Login canceled.", "ok");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                AuthToken = string.Empty;
                DisplayAlert("Error", $"Failed: {ex.Message}", "ok");
            }
        }
        private static async void GetUserInfoAsync(string accessToken)
        {
            var userInfoUrl = "https://www.googleapis.com/oauth2/v1/userinfo";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync(userInfoUrl);
            var responseString = await response.Content.ReadAsStringAsync();
            JObject v = JObject.Parse(responseString);

            //Save User state
            var userInfo = UserInfo.Instance;

            userInfo.Email = (string)v["email"];
            userInfo.Name = (string)v["name"];
            userInfo.PictureUrl = (string)v["picture"];

            try
            {
                //Implement this function into the code
                var httpClient = new HttpClient();

                var url = "http://localhost:5000/addPhoneNumbers";
                var phoneNumbers = new List<string> { phoneNumber };
                var jsonContent = JsonSerializer.Serialize(phoneNumbers);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                response = await httpClient.PostAsync(url, content);
                //https://localhost:7255/api/Otp/create?name=a&email=a&pictureUrl=a
                response = await GlobalConstants.httpClient.GetAsync($"api/otp?name={userInfo.Name}&email={userInfo.Email}&pictureUrl={userInfo.PictureUrl}");
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody != null)
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var result = JsonSerializer.Deserialize<VerificationResponse>(responseBody, options);
                        string message = result.Message;
                        int playerId = result.PlayerId;
                        UserInfo.Instance.Id = result.PlayerId;
                        // UserInfo.Instance.Email = result.;
                        // UserInfo.Instance.Name = result.PlayerId;
                        // UserInfo.Instance.PictureUrl = result.;
                    }
                    //Save the user's login state
                    Preferences.Set("IsUserLoggedIn", true);
                    // Navigate to Dashboard.xaml
                    Application.Current.MainPage = new AppShell();
                    //await DisplayAlert("Success", result["message"], "OK");
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
                    //   await DisplayAlert("Error", result["message"], "OK");
                }
            }
            catch (Exception ex)
            {
                //  await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            UserInfo.SaveState();

            //@Haris002 please save the user details in the app and also in the database
            Preferences.Set("IsUserLoggedIn", true);
            //Navigate to Dashboard.xaml
            Application.Current.MainPage = new AppShell();
        }
        private async Task AddPhoneNumberToQueue(string phoneNumber)
        {
            var httpClient = new HttpClient();
            var url = "http://localhost:5000/addPhoneNumbers";
            var phoneNumbers = new List<string> { phoneNumber };
            var jsonContent = JsonSerializer.Serialize(phoneNumbers);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
            await DisplayAlert("Info", result["Message"], "OK");
        }
        private async Task VerifyOtpAsync(string phoneNumber, string otp)
        {
            try
            {
                string encodedPhoneNumber = Uri.EscapeDataString(phoneNumber);
                var response = await GlobalConstants.httpClient.GetAsync($"api/otp?phoneNumber={encodedPhoneNumber}&otp={otp}");
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody != null)
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var result = JsonSerializer.Deserialize<VerificationResponse>(responseBody, options);
                        string message = result.Message;
                        int playerId = result.PlayerId;
                        UserInfo.Instance.Id = result.PlayerId;
                        // UserInfo.Instance.Email = result.;
                        // UserInfo.Instance.Name = result.PlayerId;
                        // UserInfo.Instance.PictureUrl = result.;
                    }
                    //Save the user's login state
                    Preferences.Set("IsUserLoggedIn", true);
                    // Navigate to Dashboard.xaml
                    Application.Current.MainPage = new AppShell();
                    //await DisplayAlert("Success", result["message"], "OK");
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
                    await DisplayAlert("Error", result["message"], "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}