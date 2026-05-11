using Acr.UserDialogs;
using LudoClient.Constants;
using LudoClient.Services;
using SharedCode.Constants;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {
        private bool _isLoggingIn = false;
        public LoginPage()
        {
            InitializeComponent();

            string build = VersionTracking.CurrentBuild;
            VersionText.Text = "Version : " + build;
        }
        private async void GuestSignup_Clicked(object sender, EventArgs e)
        {
            if (_isLoggingIn)
                return;
            _isLoggingIn = true;
            try
            {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await performLoginAsync("Guest3");
            }
            finally
            {
                _isLoggingIn = false;
            }
            return;

        }
        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
            if (_isLoggingIn)
                return;
            _isLoggingIn = true;
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            try
            {
#if WINDOWS
                await performLoginAsync("Guest3");
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
            }
            finally
            {
#if ANDROID
                UserDialogs.Instance.HideLoading();
#endif
                _isLoggingIn = false;
            }
        }
        private async Task performLoginAsync(String idToken)
        {
            try
            {
                UserInfo.Instance.player = await GlobalConstants.MatchMaker.GoogleAuthentication(idToken).ConfigureAwait(false);

                if (UserInfo.Instance.player != null)
                {
                    UserInfo.SaveState();
                    _ = GlobalConstants.MatchMaker.ConnectAsync();

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
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
