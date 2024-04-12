using System.ComponentModel;

namespace LudoClient
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
            //MainPage = new DailyBonusPage();
            
            //MainPage = new DashboardPage();
            //MainPage = new TabHandeler();
        }
#if WINDOWS
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            const int newWidth = 400;
            const int newHeight = 800;

            window.Width = newWidth;
            window.Height = newHeight;
            window.X = 0;
            window.Y = 40;
            window.Destroying += Window_Destroying;
            return window;
        }

        private void Window_Destroying(object sender, EventArgs e)
        {
            Window? window = sender as Window;
            try
            {
                System.Diagnostics.Debug.WriteLine(window.X+"Destroying"+ window.Y);
            }
            catch (Exception)
            {
            }
        }
#endif
    }
}
