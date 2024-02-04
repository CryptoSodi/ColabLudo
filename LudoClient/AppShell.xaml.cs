namespace LudoClient
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            var getuserSavedkey = Preferences.Get("UserAlreadyloggedIn", false);
         
            if (!getuserSavedkey)
            {
                // If user is not logged in, navigate to LoginPage
                Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
                GoToAsync($"//{nameof(LoginPage)}");
            }
            else
            {
                // If user is already logged in, navigate to MainPage
                Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
                GoToAsync($"//{nameof(DashboardPage)}");
            }
        }           
    }
}
