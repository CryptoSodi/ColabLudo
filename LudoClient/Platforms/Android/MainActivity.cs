using Acr.UserDialogs;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Java.Security;
using LudoClient.Platforms.Android;
using LudoClient.Services;
using PMSignature = Android.Content.PM.Signature;
using PMPermission = Android.Content.PM.Permission;


namespace LudoClient
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        const int RequestCameraId = 2000;
        [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
        [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "xamarinessentials")]
        public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
        {}

        private IGamepadInputService? _input;
        private Psg1InputOptions _options = new() { DeadZone = 0.15f };
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 9001 && GoogleAuthService.Instance != null && data != null)
            {
                GoogleAuthService.Instance.OnActivityResult(data);
            }

            // No more MwaService, but maybe we'll need this for WalletConnection if it uses StartActivityForResult
            // For now, WalletConnection seems to use a background loop, but we might need to handle activity results for intents.
        }

        [Obsolete]
        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
                string sha1 = GetApkSignatureSha1(this);                
                Console.WriteLine($"My APK SHA-1 = {sha1}");//
                UserDialogs.Init(this);
                // Resolve via MAUI's global ServiceProvider (avoid your own Services namespace)
                var sp = MauiApplication.Current.Services;
                _input = sp.GetRequiredService<IGamepadInputService>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Global Exception] {ex}");
                // Show a user-friendly error page or restart app gracefully
            }
            System.Diagnostics.Debug.WriteLine("MainActivity OnCreate - checking permissions");
            try
            {
                // Check and request permissions using native Android system
                if (CheckSelfPermission(global::Android.Manifest.Permission.Camera) != PMPermission.Granted ||
                    CheckSelfPermission(global::Android.Manifest.Permission.RecordAudio) != PMPermission.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("Requesting camera and microphone permissions");
                    RequestPermissions(new string[]
                    {
                    global::Android.Manifest.Permission.Camera,
                    global::Android.Manifest.Permission.RecordAudio
                    }, RequestCameraId);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ Camera and microphone permissions already granted");
                }
            }
            catch (Exception)
            {
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, PMPermission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestCameraId)
            {
                bool cameraGranted = false;
                bool microphoneGranted = false;

                for (int i = 0; i < permissions.Length; i++)
                {
                    if (permissions[i] == global::Android.Manifest.Permission.Camera)
                    {
                        cameraGranted = grantResults[i] == PMPermission.Granted;
                        System.Diagnostics.Debug.WriteLine($"Camera permission: {cameraGranted}");
                    }
                    else if (permissions[i] == global::Android.Manifest.Permission.RecordAudio)
                    {
                        microphoneGranted = grantResults[i] == PMPermission.Granted;
                        System.Diagnostics.Debug.WriteLine($"Microphone permission: {microphoneGranted}");
                    }
                }

                if (cameraGranted && microphoneGranted)
                {
                    System.Diagnostics.Debug.WriteLine("✅ All permissions granted");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Some permissions denied");
                }
            }
        }
        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            if (!IsGamepad(e.Device)) return base.OnKeyDown(keyCode, e);
            var btn = MapKeycode(keyCode);
            if (btn != null)
            {
                _input?.OnButtonChanged(e.Device?.Name ?? "Unknown", btn, true);
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            if (!IsGamepad(e.Device)) return base.OnKeyUp(keyCode, e);
            var btn = MapKeycode(keyCode);
            Console.WriteLine($"$BUTTON {btn}");
            if (btn != null)
            {
                _input?.OnButtonChanged(e.Device?.Name ?? "Unknown", btn, false);
                return true;
            }
            return base.OnKeyUp(keyCode, e);
        }

        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            if (!IsGamepad(e.Device)) return base.OnGenericMotionEvent(e);

            // ✅ Use Android.Views.Axis (not MotionEvent.Axis*)
            ReadAxis(e, Axis.X, "LeftStickX");
            ReadAxis(e, Axis.Y, "LeftStickY");

            // Right stick: try both sets to be safe across devices
            ReadAxis(e, Axis.Z, "RightStickX");
            ReadAxis(e, Axis.Rz, "RightStickY");
            ReadAxis(e, Axis.Rx, "RightStickX");
            ReadAxis(e, Axis.Ry, "RightStickY");

            // D-pad as hat axes (some devices)
            ReadAxis(e, Axis.HatX, "DpadX");
            ReadAxis(e, Axis.HatY, "DpadY");

            return true; // consumed
        }

        void ReadAxis(MotionEvent e, Axis axis, string name)
        {
            float v = e.GetAxisValue(axis);

           // Console.WriteLine($"axis {axis}");
            if (Math.Abs(v) < _options.DeadZone) v = 0f;
            _input?.OnAxisChanged(e.Device?.Name ?? "Unknown", name, v);
        }

        static bool IsGamepad(InputDevice? d)
        {
            if (d == null) return false;
            var s = d.Sources;
            return s.HasFlag(InputSourceType.Gamepad)
                || s.HasFlag(InputSourceType.Joystick)
                || s.HasFlag(InputSourceType.Dpad);
        }

        static string? MapKeycode(Keycode code) => code switch
        {
            // D-Pad
            Keycode.DpadUp => "DpadUp",
            Keycode.DpadDown => "DpadDown",
            Keycode.DpadLeft => "DpadLeft",
            Keycode.DpadRight => "DpadRight",
            Keycode.DpadCenter => "DpadCenter",

            // Face buttons
            Keycode.ButtonA => "A",
            Keycode.ButtonB => "B",
            Keycode.ButtonX => "X",
            Keycode.ButtonY => "Y",

            // Shoulders
            Keycode.ButtonL1 => "L1",
            Keycode.ButtonR1 => "R1",

            // Start/Select/Back
            Keycode.ButtonStart => "Start",
            Keycode.ButtonSelect => "Select",
            Keycode.Back => "Back",

            // Some pads emit these; PSG1 may not have physical L2/R2
            Keycode.ButtonL2 => "L2",
            Keycode.ButtonR2 => "R2",
            _ => null
        };
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

        private class Psg1InputOptions
        {
            public float DeadZone { get; set; }
        }
    }
}
