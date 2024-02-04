namespace LudoClient
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            count = 100;
            // Execute your function when the page is loaded
            // For example:
            // MyFunction();
        }
        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            Preferences.Set("UserAlreadyloggedIn", false);

            Shell.Current.GoToAsync(state: "//LoginPage");
            
            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);

        }
    }

}
