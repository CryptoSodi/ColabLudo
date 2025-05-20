using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Games;        // PlayGames, GamesSignInClient, AuthenticationResult
using Android.Gms.Tasks;        // Task, IOnCompleteListener
using Android.OS;
using Android.Runtime;
using System;

namespace LudoClient
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
        [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "xamarinessentials")]
        public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
        {
        }

        IGamesSignInClient _signInClient;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Obtain the Play Games sign-in client
            _signInClient = PlayGames.GetGamesSignInClient(this);

            // 1) Silent sign-in on startup
            _signInClient.IsAuthenticated()
                         .AddOnCompleteListener(new AuthListener(this));
        }
        /// <summary>
        /// Call this from your “Sign In” button to re-prompt the user.
        /// </summary>
        public void SignInWithPlayGames()
        {
            // 2) Manual sign-in if silent check failed
            _signInClient.SignIn()
                         .AddOnCompleteListener(new AuthListener(this));
        }
        /// <summary>
        /// Handles both silent and manual sign-in results.
        /// </summary>
        class AuthListener : Java.Lang.Object, IOnCompleteListener
        {
            readonly MainActivity _activity;
            public AuthListener(MainActivity activity) => _activity = activity;

            public void OnComplete(Android.Gms.Tasks.Task task)
            {
                // If the Task failed (e.g. network), show sign-in UI
                if (!task.IsSuccessful)
                {
                    _activity.ShowSignInButton();
                    return;
                }

                // Cast to AuthenticationResult to check status
                var authResult = task.Result.JavaCast<AuthenticationResult>();
                if (authResult.IsAuthenticated)
                {
                    _activity.OnSignedIn();  // user is signed in
                }
                else
                {
                    _activity.ShowSignInButton();  // prompt user
                }
            }
        }

        // TODO: wire these up in your UI
        void ShowSignInButton() { /* enable your button → calls SignInWithPlayGames() */ }
        void OnSignedIn() { /* proceed with signed-in logic */ }
    }
}