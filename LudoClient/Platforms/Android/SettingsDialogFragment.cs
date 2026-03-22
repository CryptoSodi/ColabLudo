using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using LudoClient.Constants;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;
using LudoClient.Services;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using SharedCode.Constants;
using LudoClient;

namespace LudoClient.Platforms.Android
{
    public class SettingsDialogFragment : DialogFragment
    {
        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Remove dialog background so our rounded corner background shows
            if (Dialog != null && Dialog.Window != null)
            {
                Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));
                Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            }

            var view = inflater.Inflate(Resource.Layout.dialog_settings, container, false);

            var soundSwitch = view.FindViewById<SettingsSwitchView>(Resource.Id.soundSwitch);
            var vibrationSwitch = view.FindViewById<SettingsSwitchView>(Resource.Id.vibrationSwitch);
            var notificationSwitch = view.FindViewById<SettingsSwitchView>(Resource.Id.notificationSwitch);

            soundSwitch.Init("IsSoundEnabled", "SOUNDS");
            vibrationSwitch.Init("IsVibrationEnabled", "VIBRATION");
            notificationSwitch.Init("IsNotificationEnabled", "NOTIFICATION");

            var versionText = view.FindViewById<TextView>(Resource.Id.versionText);
            versionText.Text = "Version : " + VersionTracking.CurrentBuild;

            var btnSignOut = view.FindViewById<global::Android.Views.View>(Resource.Id.btnSignOut);
            btnSignOut.Click += SignOutTapped;

            var btnHelp = view.FindViewById<global::Android.Widget.ImageButton>(Resource.Id.settingsBtnHelp);
            btnHelp.Click += OnHelpTapped;

            // Stop the stopwatch, similar to Settings.xaml.cs OnAppearing
            if (ClientGlobalConstants.sw != null)
            {
                ClientGlobalConstants.sw.Stop();
                Console.WriteLine($"CashGame full load time: {ClientGlobalConstants.sw.ElapsedMilliseconds} ms");
            }

            return view;
        }

        private void OnHelpTapped(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Dismiss();
            Microsoft.Maui.Controls.Application.Current?.MainPage.ShowPopup(ClientGlobalConstants.helpDesk, new PopupOptions { Shape = null });
        }

        private void SignOutTapped(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            
            try
            {
                var authService = DependencyService.Get<IGoogleAuthService>();
                if (authService != null)
                {
                    authService.SignOutAsync().ContinueWith(task =>
                    {
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (task.IsCompletedSuccessfully && task.Result)
                            {
                            }
                            else
                            {
                                // Sign-out failed
                            }
                        });
                    });
                }
            }
            catch (Exception)
            {   
            }
            
            Dismiss();
            UserInfo.Logout();
            Microsoft.Maui.Controls.Application.Current.MainPage = new LoginPage();
        }
    }
}