using Newtonsoft.Json.Linq;
using SharedCode.Constants;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {
        const string authenticationUrl = "https://xamarin-essentials-auth-sample.azurewebsites.net/mobileauth/";
        const string userInfoUrl = "https://www.googleapis.com/oauth2/v1/userinfo";
        OtpRequest otpReuest = null;
        public LoginPage()
        {
            InitializeComponent();
            GetCountryByIpAsync();
        }
        private async Task AddPhoneNumberToQueue()
        {
            if (UserInfo.Instance.Id != null)
                otpReuest.playerId = UserInfo.Instance.Id + "";
            var content = new StringContent(JsonSerializer.Serialize(otpReuest), Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await GlobalConstants.httpClient.PostAsync("api/Otp", content);
                string responseBody = await response.Content.ReadAsStringAsync();
                if (responseBody.Contains("OTP saved successfully."))
                {
                    Console.WriteLine($"Status Code: {response.StatusCode}");
                    Console.WriteLine("Response Body:");
                    Console.WriteLine(responseBody);
                    NumberField.IsVisible = false;
                    OtpField.IsVisible = true;
                    BtnCancel.IsVisible = true;
                    BtnLoginSingup.Source = "abtnsignup.png";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
            }
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



            if (otpReuest.country == "")
                otpReuest.country = result["country"].GetString();

            UserInfo.Instance.Id = playerId;
            UserInfo.Instance.PhoneNumber = phoneNumber;
            UserInfo.Instance.Country = otpReuest.country;
            UserInfo.Instance.CountryCode = otpReuest.countryCode;
            UserInfo.Instance.RegionName = otpReuest.regionName;
            UserInfo.Instance.City = otpReuest.city;
            UserInfo.Instance.Lat = (float)otpReuest.lat;
            UserInfo.Instance.Lon = (float)otpReuest.lon;

            UserInfo.Instance.Name = PlayerName;
            UserInfo.Instance.Email = Email;
            UserInfo.Instance.PictureUrl = PlayerPicture;
            UserInfo.Instance.Coins = (float)PlayerLudoCoins;
            UserInfo.Instance.PlayerCryptoCoins = (float)PlayerCryptoCoins;
        }
        private async Task VerifyOtpAsync(string phoneNumber, string otp)
        {
            try
            {
                var response = await GlobalConstants.httpClient.GetAsync($"api/otp?phoneNumber={Uri.EscapeDataString(phoneNumber)}&otp={otp}");
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Dictionary<string, JsonElement>? result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);

                    SetupInstance(result);

                    string message = result["message"].GetString();
                    if (message.Contains("success"))
                    {
                        if (UserInfo.Instance.Name == null)
                        {
                            NumberField.IsVisible = false;
                            OtpField.IsVisible = false;
                            BtnLoginSingup.IsVisible = false;
                            BtnCancel.IsVisible = false;
                            await DisplayAlert("Success", "Please Link a Google account to this number.", "OK");
                        }
                        else
                        {
                            UserInfo.SaveState();
                            //Success Login
                            Application.Current.MainPage = new AppShell();
                        }
                    }
                    else
                        await DisplayAlert("Failed", message, "OK");
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
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // A simple validation regex for phone numbers. This can be improved based on requirements.
            //return Regex.IsMatch(phoneNumber, @"^\+[1-9]\d{1,14}$");
            return Regex.IsMatch(phoneNumber, @"^[0-9]{7,15}$");
        }
        private async void LoginSingup_Clicked(object sender, EventArgs e)
        {
            String Number = NumberField.entryField.Text;
            String OTP = OtpField.entryField.Text;
            if (BtnLoginSingup.Source is FileImageSource fileImageSource && fileImageSource.File == "abtnlogin.png")
            {
                otpReuest.phoneNumber = Number;
                //Perform Login
                AddPhoneNumberToQueue();
            }
            else
            {
                if (string.IsNullOrEmpty(OTP))
                {
                    await DisplayAlert("Error", "Please enter OTP.", "OK");
                    return;
                }
                // Call the API to verify OTP
                await VerifyOtpAsync(Number, OTP);
            }
            //GooleSignup_Clicked(null, null);
        }
        private async void Cancel_Clicked(object sender, EventArgs e)
        {
            NumberField.IsVisible = true;
            OtpField.IsVisible = false;
            BtnCancel.IsVisible = false;
            BtnLoginSingup.Source = "abtnlogin.png";
            NumberField.entryField.Text = otpReuest.countryCode;
        }
        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
#if WINDOWS
            UserInfo.Instance.Email = "Sodi@gmail.com";
            UserInfo.Instance.Name = "Sodi";
            UserInfo.Instance.PictureUrl = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";
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
         try
            {    
                if (GlobalConstants.online)
                {
                    string url = "api/GoogleAuthentication?name=" + UserInfo.Instance.Name + "&email=" + UserInfo.Instance.Email + "&pictureUrl=" + UserInfo.Instance.PictureUrl;
                    if (UserInfo.Instance.Id != null)
                        url = url + "&playerId=" + UserInfo.Instance.Id;

                    HttpResponseMessage response = await GlobalConstants.httpClient.PostAsync(url, null);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Dictionary<string, JsonElement>? result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);

                    SetupInstance(result);
                    string message = result["message"].GetString();

                    response.EnsureSuccessStatusCode();
                    if (message == "Player login successfully." || message == "Player updated successfully.")
                    {
                        //Save the user's login state
                        UserInfo.SaveState();
                        //Hide Loader
                        Application.Current.MainPage = new AppShell();
                    }
                    else if (message == "Player created successfully." || message == "Attach Phone.")
                    {
                        NumberField.IsVisible = true;
                        OtpField.IsVisible = false;
                        BtnLoginSingup.IsVisible = true;
                        BtnCancel.IsVisible = false;
                        GoogleLoginPanel.IsVisible = false;
                        await DisplayAlert("Success", "Please Link a your phone number.", "OK");
                    }
                }
           }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
        public class OtpRequest
        {
            public string playerId { get; set; }
            public string phoneNumber { get; set; }
            public string countryCode { get; set; }
            public string country { get; set; }
            public string regionName { get; set; }
            public string city { get; set; }
            public double lat { get; set; }
            public double lon { get; set; }
        }

        public async void GetCountryByIpAsync()
        {
            try
            {
                using HttpClient client = new HttpClient();
                //{"status":"success","country":"Pakistan","countryCode":"PK","region":"PB","regionName":"Punjab","city":"Lahore","zip":"54020","lat":31.558,"lon":74.3587,"timezone":"Asia/Karachi","isp":"Cloudflare, Inc.","org":"Cloudflare WARP","as":"AS13335 Cloudflare, Inc.","query":"104.28.212.126"}
                string url = "http://ip-api.com/json/?token=0cf62c767a1ab9";

                var response = await client.GetStringAsync(url);
                otpReuest = JsonSerializer.Deserialize<OtpRequest>(response);
                otpReuest.countryCode = otpReuest?.country == "Pakistan" ? "+92" : "+91";
                NumberField.entryField.Text = otpReuest.countryCode;
                otpReuest.country = otpReuest?.country;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}