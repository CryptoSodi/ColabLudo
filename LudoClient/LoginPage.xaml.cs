
using LudoClient.Models;
using Microsoft.Maui.Controls;
using Newtonsoft.Json.Linq;
using SharedCode.Constants;
using System.Text.Json;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {
        const string authenticationUrl = "https://xamarin-essentials-auth-sample.azurewebsites.net/mobileauth/";
        const string userInfoUrl = "https://www.googleapis.com/oauth2/v1/userinfo";
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void LoginSingup_Clicked(object sender, EventArgs e)
        {
            String Number = NumberField.entryField.Text;
            String OTP = OtpField.entryField.Text;
            if (BtnLoginSingup.Source is FileImageSource fileImageSource && fileImageSource.File == "abtnlogin.png")
            {
                //Perform Login
                
            }
            GooleSignup_Clicked(null, null);
        }
        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
#if WINDOWS
            UserInfo.Instance.Email = "Mazhar@gmail.com";
            UserInfo.Instance.Name = "Mazhar";
            UserInfo.Instance.PictureUrl = "https://cdn.icon-icons.com/icons2/2643/PNG/512/male_boy_person_people_avatar_white_tone_icon_159368.png";
            performLoginAsync();
#endif
#if ANDROID
            try
            {
                OnAuthenticate("Google");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await DisplayAlert("Error", "Failed to sign in: " + ex.Message, "OK");
            }
#endif
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
        private async void GetUserInfoAsync(string accessToken)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync(userInfoUrl);
            String responseString = await response.Content.ReadAsStringAsync();

            JObject googleResponse = JObject.Parse(responseString);
            UserInfo.Instance.Email = (string)googleResponse["email"];
            UserInfo.Instance.Name = (string)googleResponse["name"];
            UserInfo.Instance.PictureUrl = (string)googleResponse["picture"];
            performLoginAsync();
        }
        private async void performLoginAsync()
        {
            string responseBody = null;
            try
            {
                if (GlobalConstants.online)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, GlobalConstants.BaseUrl + "api/GoogleAuthentication?name=" + UserInfo.Instance.Name + "&email=" + UserInfo.Instance.Email + "&pictureUrl=" + UserInfo.Instance.PictureUrl);
                    var response = await GlobalConstants.httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    responseBody = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        if (responseBody != null)
                        {
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            var result = JsonSerializer.Deserialize<VerificationResponse>(responseBody, options);
                            //string message = result.Message;

                            UserInfo.Instance.Id = result.PlayerId;
                            //Save the user's login state
                            UserInfo.SaveState();
                            //Hide Loader
                            Application.Current.MainPage = new AppShell();
                        }
                    }
                    else
                    {
                        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
                        await DisplayAlert("Error", result["message"], "OK");
                    }
                }
                else
                {
                    UserInfo.Instance.Id = 1;
                    //Save the user's login state
                    UserInfo.SaveState();
                    //Hide Loader
                    Application.Current.MainPage = new AppShell();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}