using LudoClient.Utilities;
using Microsoft.Maui.Controls;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {
        private string phoneNumber;
        private string expectedOTP;

        public LoginPage()
        {
            InitializeComponent();
            OTPPanel.IsVisible = true;
            LoginPanel.IsVisible = false;
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Execute your function when the page is loaded
            // For example:
            // MyFunction();
        }

        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
            try
            {
                SemanticScreenReader.Announce(CounterBtn.Text);
                OnAuthenticate("Google");
                /*/ Generate state
                state = GenerateState();
                string clientId = "497194700135-108a3be43e3kv3oae82pdtl9d1ju9dom.apps.googleusercontent.com";
                string redirectUri = "http://localhost:5000";
                string authorizationUrl = $"https://accounts.google.com/o/oauth2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=openid%20https://www.googleapis.com/auth/userinfo.email%20profile%20email&response_type=code&access_type=offline&hl=en&state={state}";


               // var result = await WebAuthenticator.AuthenticateAsync(new Uri(authorizationUrl), new Uri(redirectUri));

                await Browser.OpenAsync(authorizationUrl, BrowserLaunchMode.SystemPreferred);

                // Wait for redirect and handle auth
                HandleGoogleAuth();
            */
            }
            catch (Exception ex)
            {
                // Handle or log the error
                await DisplayAlert("Error", "Failed to sign in: " + ex.Message, "OK");
            }
        }

        private static string GenerateState()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var byteArray = new byte[32];
                rng.GetBytes(byteArray);
                return Convert.ToBase64String(byteArray);
            }
        }
        const string authenticationUrl = "https://xamarin-essentials-auth-sample.azurewebsites.net/mobileauth/";
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

        private static async Task<JObject> GetUserInfoAsync(string accessToken)
        {
            var userInfoUrl = "https://www.googleapis.com/oauth2/v1/userinfo";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync(userInfoUrl);
            var responseString = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseString);
        }

        private void SendOTP_Clicked(object sender, EventArgs e)
        {
            // Get the entered phone number
            phoneNumber = PhoneNumberEntry.Text;
            // Generate and send OTP (You would use a service for this in a real application)
            // For now, you might simulate it or display a message.
            expectedOTP = GenerateOTP(); // You need to implement this method
            // Hide the login panel
            HideLoginPanel();
            // Show OTP-related components
            OTPPanel.IsVisible = true;
            LoginPanel.IsVisible = false;
        }

        private void HideLoginPanel()
        {
            // Hide the login panel and reset state
            PhoneNumberEntry.Text = string.Empty;
            OTPPanel.IsVisible = false;
        }

        private void VerifyOTP_Clicked(object sender, EventArgs e)
        {
            string enteredOTP = OTPEnter.Text;

            if (enteredOTP == expectedOTP)
            {
                LoginPanel.IsVisible = true;

                DisplayAlert("Success", "OTP Verified", "OK");
                // TODO: Perform further actions after successful OTP verification
                Preferences.Set("UserAlreadyloggedIn", true);

                Shell.Current.GoToAsync(state: "//MainPage");
            }
            else
            {
                DisplayAlert("Error", "Invalid OTP", "OK");
            }
        }

        private void Cancel_Clicked(object sender, EventArgs e)
        {
            // Hide OTP-related components and reset state
            OTPPanel.IsVisible = false;
            OTPEnter.Text = string.Empty;
            LoginPanel.IsVisible = true;

            // Show the login panel
            ShowLoginPanel();
        }

        private void ShowLoginPanel()
        {
            // Show the login panel
            OTPPanel.IsVisible = false;
        
        
        }

        // Simulate OTP generation (replace with a proper implementation)
        private string GenerateOTP()
        {
            return "123456"; // Example OTP
        }
    }
}
