using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Tasks;  // for IOnCompleteListener and Android.Gms.Tasks.Task
using Android.Provider;
using LudoClient.Services;
namespace LudoClient.Platforms.Android
{
    public class DeviceIdentifierService : IDeviceIdentifierService
    {
        public string GetDeviceId()
        {
            var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
            return Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId);
        }
    }
    public class GoogleAuthService : Java.Lang.Object, IGoogleAuthService
    {
        private TaskCompletionSource<string?> _signInTcs;
        private TaskCompletionSource<bool> _signOutTcs;
        public static GoogleAuthService? Instance { get; private set; }
        public GoogleAuthService()
        {
            Instance = this;
        }
        public string? UserName { get; private set; }
        public string? UserEmail { get; private set; }
        public string? UserPhotoUrl { get; private set; }
        public Task<string?> SignInAsync()
        {
            _signInTcs = new TaskCompletionSource<string?>();

            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken("973406093603-g14f7hkjafphcij4p16ectibrkmj7q8f.apps.googleusercontent.com") // Replace this
                .RequestEmail()
                .RequestProfile()
                .Build();

            var signInClient = GoogleSignIn.GetClient(Platform.CurrentActivity, gso);
            var signInIntent = signInClient.SignInIntent;

            Platform.CurrentActivity.StartActivityForResult(signInIntent, 9001);

            return _signInTcs.Task;
        }
        public Task<bool> SignOutAsync()
        {
            _signOutTcs = new TaskCompletionSource<bool>();

            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken("973406093603-g14f7hkjafphcij4p16ectibrkmj7q8f.apps.googleusercontent.com") // same web client ID here
                .RequestEmail()
                .Build();

            var signInClient = GoogleSignIn.GetClient(Platform.CurrentActivity, gso);

            signInClient.SignOut()
                .AddOnCompleteListener(new OnCompleteListener(task =>
                {
                    if (task.IsSuccessful)
                    {
                        _signOutTcs.TrySetResult(true);
                    }
                    else
                    {
                        _signOutTcs.TrySetException(new Exception("Google Sign-Out failed."));
                    }
                }));

            return _signOutTcs.Task;
        }
        public void OnActivityResult(Intent data)
        {
            var task = GoogleSignIn.GetSignedInAccountFromIntent(data);
            if (task.IsSuccessful)
            {
                GoogleSignInAccount account = (GoogleSignInAccount)task.Result;

                UserName = account.GivenName;
                UserEmail = account.Email;
                UserPhotoUrl = account.PhotoUrl?.ToString();

                _signInTcs.TrySetResult(account?.IdToken);
            }
            else
            {
                _signInTcs.TrySetException(new System.Exception("Google Sign-In failed."));
            }
        }

        private class OnCompleteListener : Java.Lang.Object, IOnCompleteListener
        {
            private readonly Action<global::Android.Gms.Tasks.Task> _onComplete;

            public OnCompleteListener(Action<global::Android.Gms.Tasks.Task> onComplete)
            {
                _onComplete = onComplete;
            }
            public void OnComplete(global::Android.Gms.Tasks.Task task)
            {
                _onComplete?.Invoke(task);
            }
        }
    }
}
