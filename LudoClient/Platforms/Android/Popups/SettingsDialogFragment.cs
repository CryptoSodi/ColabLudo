using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Fragment.App;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using LudoClient;
using LudoClient.Constants;
using LudoClient.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using SharedCode.Constants;
using System;
using System.Threading.Tasks;

namespace LudoClient.Platforms.Android.Popups
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

        //    var btnHelp = view.FindViewById<global::Android.Widget.ImageButton>(Resource.Id.settingsBtnHelp);
        //    btnHelp.Click += OnHelpTapped;

            // Stop the stopwatch, similar to Settings.xaml.cs OnAppearing
            if (ClientGlobalConstants.sw != null)
            {
                ClientGlobalConstants.sw.Stop();
                Console.WriteLine($"CashGame full load time: {ClientGlobalConstants.sw.ElapsedMilliseconds} ms");
            }
            ConstraintLayout settingsContainer = view.FindViewById<ConstraintLayout>(Resource.Id.settingsContainer);
            ImageView bgImage = view.FindViewById<ImageView>(Resource.Id.settingsforground);

            bgImage.Post(() =>
            {
                int width = bgImage.Width;
                int height = bgImage.Height;

                var layoutParams = (FrameLayout.LayoutParams)settingsContainer.LayoutParameters;

                layoutParams.Width = (int)(width);
                layoutParams.Height = (int)(height);

                // ✅ Center inside FrameLayout
                layoutParams.Gravity = GravityFlags.Top;


                settingsContainer.LayoutParameters = layoutParams; // ✅ apply to SAME view
            });
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