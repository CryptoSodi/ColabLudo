using Acr.UserDialogs;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Java.Security;
using LudoClient.Platforms.Android;
using PMSignature = Android.Content.PM.Signature;

namespace LudoClient
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
        [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "xamarinessentials")]
        public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
        {}

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 9001 && GoogleAuthService.Instance != null && data != null)
            {
                GoogleAuthService.Instance.OnActivityResult(data);
            }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            string sha1 = GetApkSignatureSha1(this);
            Console.WriteLine($"My APK SHA-1 = {sha1}");
            UserDialogs.Init(this);
        }
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