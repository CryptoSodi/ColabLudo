using LudoClient.Utilities;
using SimpleToolkit.Core;

namespace LudoClient
{
    public partial class AppShell : SimpleToolkit.SimpleShell.SimpleShell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));            

            AddTab(typeof(DashboardPage), PageType.HomePage);
            AddTab(typeof(FriendsPage), PageType.FriendsPage);
            AddTab(typeof(WalletPage), PageType.WalletPage);
            AddTab(typeof(LeaderboardPage), PageType.LeaderboardPage);

            Loaded += (s, e) =>
            {
                AppShellLoaded(s, e);
                try
                {
                    _ = Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Navigation error]: {ex.Message}");
                }
            };
        }
        private static void AppShellLoaded(object sender, EventArgs e)
        {
            var shell = sender as AppShell;
            if (shell != null)
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
            try
            {
                Shell.Current.GoToAsync("///" + e.CurrentPage.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Navigation error]: {ex.Message}");
            }
        }
    }
    public enum PageType
    {
        HomePage, FriendsPage, WalletPage, LeaderboardPage
    }
}
