using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Java.Security;
using PMSignature = Android.Content.PM.Signature;
using Android.Gms.Games;        // PlayGames, GamesSignInClient, AuthenticationResult
using Android.Gms.Tasks;        // Task, IOnCompleteListener
using Android.OS;
using Android.Runtime;
using System;
using Android.Gms.Auth.Api.SignIn;
using System.Net;

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
            string sha1 = GetApkSignatureSha1(this);
            Console.WriteLine($"My APK SHA-1 = {sha1}");



            // Obtain the Play Games sign-in client
            _signInClient = PlayGames.GetGamesSignInClient(this);

            // 1) Silent sign-in on startup
            _signInClient.IsAuthenticated()
                         .AddOnCompleteListener(new AuthListener(this));
            
            
            System.Threading.Tasks.Task.Run(() => {
                System.Threading.Tasks.Task.Delay(5000);
                SignInWithPlayGames(); 
            } );
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



        public static string GetApkSignatureSha1(Context context)
        {
            try
            {
                var pm = context.PackageManager;
                var pkgName = context.PackageName;
                PackageInfo pkgInfo;
                PMSignature[] sigs;

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
                {
                    // API 28+ uses SigningInfo
                    pkgInfo = pm.GetPackageInfo(pkgName, PackageInfoFlags.SigningCertificates);
                    var signingInfo = pkgInfo.SigningInfo;
                    // Java getApkContentsSigners() → C# GetApkContentsSigners()
                    sigs = signingInfo.GetApkContentsSigners();
                }
                else
                {
                    // Pre-API 28 uses the old Signatures array
                    pkgInfo = pm.GetPackageInfo(pkgName, PackageInfoFlags.Signatures);
                    sigs = pkgInfo.Signatures.ToArray();
                }

                if (sigs?.Length > 0)
                {
                    // Hash the first cert’s raw bytes
                    var md = MessageDigest.GetInstance("SHA1");
                    md.Update(sigs.First().ToByteArray());
                    var digest = md.Digest();
                    // Convert to colon-delimited hex (e.g. AB:CD:EF…)
                    return string.Join(":", digest.Select(b => b.ToString("X2")));
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("SignatureHelper", ex.ToString());
            }

            return null;
        }
    }
}