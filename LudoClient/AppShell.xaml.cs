using LudoClient.Utilities;
using SimpleToolkit.Core;

namespace LudoClient
{
    public partial class AppShell : SimpleToolkit.SimpleShell.SimpleShell
    {
        public AppShell()
        {
            InitializeComponent();

            // If user is already logged in, navigate to DashboardPage
            Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
            GoToAsync($"//{nameof(DashboardPage)}");
            AddTab(typeof(DashboardPage), PageType.HomePage);
            AddTab(typeof(FriendsPage), PageType.FriendsPage);
            AddTab(typeof(WalletPage), PageType.WalletPage);
            AddTab(typeof(LeaderboardPage), PageType.LeaderboardPage);

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
        HomePage, FriendsPage, WalletPage, LeaderboardPage
    }
}
