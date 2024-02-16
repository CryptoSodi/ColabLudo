using SimpleToolkit.Core;
using LudoClient.Utilities;

namespace LudoClient
{
    public partial class AppShell : SimpleToolkit.SimpleShell.SimpleShell
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

            AddTab(typeof(DashboardPage), PageType.HomePage);
            AddTab(typeof(FriendsPage), PageType.FriendsPage);
            AddTab(typeof(WalletPage), PageType.WalletPage);
            AddTab(typeof(SettingsPage), PageType.SettingsPage);

            Loaded += AppShellLoaded;
        }

        private static void AppShellLoaded(object sender, EventArgs e)
        {
            var shell = sender as AppShell;

            shell.Window.SubscribeToSafeAreaChanges(safeArea =>
            {
                shell.pageContainer.Margin = safeArea;
                shell.tabBarView.Margin = safeArea;
                shell.bottomBackgroundRectangle.IsVisible = safeArea.Bottom > 0;
                shell.bottomBackgroundRectangle.HeightRequest = safeArea.Bottom;
            });
        }

        private void AddTab(Type page, PageType pageEnum)
        {
            Tab tab = new Tab { Route = pageEnum.ToString(), Title = pageEnum.ToString() };
            tab.Items.Add(new ShellContent { ContentTemplate = new DataTemplate(page) });

            tabBar.Items.Add(tab);
        }

        private void TabBarViewCurrentPageChanged(object sender, TabBarEventArgs e)
        {
            Shell.Current.GoToAsync("///" + e.CurrentPage.ToString());
        }
    }

    public enum PageType
    {
        HomePage, FriendsPage, WalletPage, SettingsPage
    }
}
