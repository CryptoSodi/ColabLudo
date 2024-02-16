using System.ComponentModel;

namespace LudoClient
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //MainPage = new AppShell();
            //MainPage = new SettingsPage();
            //MainPage = new TabHandeler();
            //MainPage = new DashboardPage();
            MainPage = new AppShell();
        }
#if WINDOWS
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            const int newWidth = 360;
            const int newHeight = 700;

            window.Width = newWidth;
            window.Height = newHeight;
            window.X = -10;
            window.Y = -10;
            window.Destroying += Window_Destroying;
            return window;
        }

        private void Window_Destroying(object sender, EventArgs e)
        {
            Window window = sender as Window;
            try
            {
                System.Diagnostics.Debug.WriteLine(window.X+"Destroying");
              
            }
            catch (Exception)
            {
            }
        }
#endif
    }
}
