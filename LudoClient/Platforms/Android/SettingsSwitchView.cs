using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Storage;
using System;

namespace LudoClient.Platforms.Android
{
    [Register("ludoclient.platforms.android.SettingsSwitchView")]
    public class SettingsSwitchView : FrameLayout
    {
        private TextView _settingText;
        private global::Android.Widget.ImageButton _toggleImage;
        private string _preferencesKey;
        private bool _switchState = true;

        public string SettingText
        {
            get => _settingText?.Text;
            set
            {
                if (_settingText != null)
                    _settingText.Text = value;
            }
        }

        public string PreferencesKey
        {
            get => _preferencesKey;
            set => _preferencesKey = value;
        }

        public SettingsSwitchView(Context context) : base(context)
        {
            Initialize(context);
        }

        public SettingsSwitchView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public SettingsSwitchView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        protected SettingsSwitchView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context)
        {
            var inflater = LayoutInflater.FromContext(context);
            var view = inflater.Inflate(Resource.Layout.item_settings_switch, this, true);

            _settingText = view.FindViewById<TextView>(Resource.Id.switchSettingText);
            _toggleImage = view.FindViewById<global::Android.Widget.ImageButton>(Resource.Id.switchToggleImage);

            _toggleImage.Click += OnToggleClicked;
        }

        public void Init(string preferencesKey, string settingText)
        {
            _preferencesKey = preferencesKey;
            SettingText = settingText;

            if (!string.IsNullOrEmpty(_preferencesKey))
            {
                _switchState = Preferences.Get(_preferencesKey, true);
                UpdateSwitchSource();
            }
        }

        private void OnToggleClicked(object sender, EventArgs e)
        {
            Constants.ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            
            _switchState = !_switchState;
            UpdateSwitchSource();

            if (!string.IsNullOrEmpty(_preferencesKey))
            {
                Preferences.Set(_preferencesKey, _switchState);
            }
        }

        private void UpdateSwitchSource()
        {
            int resId = _switchState ? Resource.Drawable.switch_btn_on : Resource.Drawable.switch_btn_off;
            _toggleImage.SetImageResource(resId);
        }
    }
}