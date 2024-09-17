namespace LudoClient.ControlView
{
    public partial class SettingsSwitch : ContentView
    {
        public static readonly BindableProperty SettingTextProperty = BindableProperty.Create(
            nameof(SettingText),
            typeof(string),
            typeof(SettingsSwitch),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var control = (SettingsSwitch)bindable;
                control.SettingTextView.Text = (string)newValue;
            });

        public string SettingText
        {
            get => GetValue(SettingTextProperty) as string;
            set => SetValue(SettingTextProperty, value);
        }

        public static readonly BindableProperty PreferencesKeyProperty = BindableProperty.Create(
            nameof(PreferencesKey),
            typeof(string),
            typeof(SettingsSwitch),
            default(string));

        public string PreferencesKey
        {
            get => (string)GetValue(PreferencesKeyProperty);
            set => SetValue(PreferencesKeyProperty, value);
        }

        public SettingsSwitch()
        {
            InitializeComponent();
            // Subscribe to the SwitchToggled event of the ImageSwitch control
            ImageSwitchControl.SwitchToggled += ImageSwitchControl_SwitchToggled;
        }
        public void init()
        {
            // Load the preference and set the switch state accordingly
            if (!string.IsNullOrEmpty(PreferencesKey))
            {
                ImageSwitchControl.SwitchState = Preferences.Get(PreferencesKey, true); // Default value is 'true'
                ImageSwitchControl.UpdateSwitchSource();
            }
        }

        // Save the state in preferences when toggled
        private void ImageSwitchControl_SwitchToggled(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(PreferencesKey))
            {
                Preferences.Set(PreferencesKey, ImageSwitchControl.SwitchState); // Save state
            }
        }
    }
}
