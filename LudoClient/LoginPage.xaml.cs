using LudoClient.Utilities;
using Microsoft.Maui.Controls;

namespace LudoClient
{
    public partial class LoginPage : ContentPage
    {
        private string phoneNumber;
        private string expectedOTP;
        private AuthenticationService _authService;

        public LoginPage()
        {
            InitializeComponent();
            OTPPanel.IsVisible = true;
            LoginPanel.IsVisible = false;
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            _authService = new AuthenticationService();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Execute your function when the page is loaded
            // For example:
            // MyFunction();
        }

        private async void GooleSignup_Clicked(object sender, EventArgs e)
        {
            try
            {
                await _authService.AuthenticateAsync();
                // Optionally handle post-sign-in logic here, such as navigating to a new page
            }
            catch (Exception ex)
            {
                // Handle or log the error
                await DisplayAlert("Error", "Failed to sign in: " + ex.Message, "OK");
            }
        }

        private void SendOTP_Clicked(object sender, EventArgs e)
        {
            // Get the entered phone number
            phoneNumber = PhoneNumberEntry.Text;
            // Generate and send OTP (You would use a service for this in a real application)
            // For now, you might simulate it or display a message.
            expectedOTP = GenerateOTP(); // You need to implement this method
            // Hide the login panel
            HideLoginPanel();
            // Show OTP-related components
            OTPPanel.IsVisible = true;
            LoginPanel.IsVisible = false;
        }

        private void HideLoginPanel()
        {
            // Hide the login panel and reset state
            PhoneNumberEntry.Text = string.Empty;
            OTPPanel.IsVisible = false;
        }

        private void VerifyOTP_Clicked(object sender, EventArgs e)
        {
            string enteredOTP = OTPEnter.Text;

            if (enteredOTP == expectedOTP)
            {
                LoginPanel.IsVisible = true;

                DisplayAlert("Success", "OTP Verified", "OK");
                // TODO: Perform further actions after successful OTP verification
                Preferences.Set("UserAlreadyloggedIn", true);

                Shell.Current.GoToAsync(state: "//MainPage");
            }
            else
            {
                DisplayAlert("Error", "Invalid OTP", "OK");
            }
        }

        private void Cancel_Clicked(object sender, EventArgs e)
        {
            // Hide OTP-related components and reset state
            OTPPanel.IsVisible = false;
            OTPEnter.Text = string.Empty;
            LoginPanel.IsVisible = true;

            // Show the login panel
            ShowLoginPanel();
        }

        private void ShowLoginPanel()
        {
            // Show the login panel
            OTPPanel.IsVisible = false;
        
        
        }

        // Simulate OTP generation (replace with a proper implementation)
        private string GenerateOTP()
        {
            return "123456"; // Example OTP
        }
    }
}
