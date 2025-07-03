using System.Runtime.InteropServices;
namespace SharedCode
{
    public partial class App : Application
    {  //Integrated console to the MAUI app for better debugging
    //    [DllImport("kernel32.dll")]
    //    static extern bool AllocConsole();
    //    [DllImport("kernel32.dll")]
    //    static extern bool FreeConsole();
    //    [DllImport("kernel32.dll", SetLastError = true)]
    //    static extern IntPtr GetConsoleWindow();
    //    [DllImport("user32.dll", SetLastError = true)]
    //    [return: MarshalAs(UnmanagedType.Bool)]
    //    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    //    const uint SWP_NOSIZE = 0x0001;
    //    static readonly IntPtr HWND_TOP = IntPtr.Zero;
        public App()
        {
            InitializeComponent();
            // Read command-line arguments
            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 4 && int.TryParse(args[1], out int gameIndexSave) && int.TryParse(args[2], out int windowX) && int.TryParse(args[3], out int windowY))
            {
                Console.WriteLine($"Received values: GameIndexSave={gameIndexSave}, X={windowX}, Y={windowY}");

                // Store globally
                Preferences.Set("GameIndexSave", gameIndexSave);
                Preferences.Set("windowX", windowX);
                Preferences.Set("windowY", windowY);
            }
            else
            {
                Console.WriteLine("No int value received.");
            }
            MainPage = new AppShell();
        }
#if WINDOWS
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);
            const int newWidth = 400;
            const int newHeight = 800;
            window.Width = newWidth;
            window.Height = newHeight;
            window.X = Preferences.Get("windowX", -5);                
            window.Y = Preferences.Get("windowY", 0);
            window.Destroying += Window_Destroying;
            return window;
        }
        private void Window_Destroying(object sender, EventArgs e)
        {
            Window? window = sender as Window;
            try
            {
                System.Diagnostics.Debug.WriteLine(window.X + "Destroying" + window.Y);
            }
            catch (Exception)
            {
            }
           // FreeConsole();
        }
#endif
    }
}