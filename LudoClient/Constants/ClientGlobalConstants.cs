using LudoClient.CoreEngine;
using LudoClient.Popups;
using Microsoft.Maui.Platform;
using System.Diagnostics;

namespace LudoClient.Constants
{
    public static class ClientGlobalConstants
    {
        public static Stopwatch sw;   // START TIMER
        private static HepticEngine _hepticEngine;

        public static HepticEngine hepticEngine => _hepticEngine ??= Application.Current?.Handler?.MauiContext?.Services.GetService<HepticEngine>();

        public static DashboardPage dashBoard;
        private static double width;
        private static double height;

        public static CashGame cashGame { get; set; } = new CashGame();
        public static FriendsPage friendsPage { get; set; } = new FriendsPage();
        public static PlayWithFriends playWithFriends { get; set; } = new PlayWithFriends();

        public static EditInfo editInfo;
        public static Settings settings { get; set; } = new Settings();
        public static HelpDesk helpDesk { get; set; } = new HelpDesk();
        public static DailyBonus dailyBonus { get; set; } = new DailyBonus();
        public static ProfileInfo profileInfo { get; set; } = new ProfileInfo();
        public static Results results { get; set; } = new Results();
        internal static AddCash addCash { get; set; } = new AddCash();
        internal static WithdrawPopup withdrawPopup { get; set; } = new WithdrawPopup();
        internal static MintingPage mintingPage { get; set; }=new MintingPage();

        internal static Game game;

        public static void Init()
        {
            // Optionally, force a layout pass to "warm up" each page.
            // You may use known dimensions or the dimensions of the current MainPage.
            // Here we assume some default width and height; adjust as needed.
            width = Application.Current.MainPage.Width > 0
                ? Application.Current.MainPage.Width
                : DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            height = Application.Current.MainPage.Height > 0
                ? Application.Current.MainPage.Height
                : DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
            // DAILY BONUS IMAGE WARMUP
            WarmImage("support_popup_bg.webp");
            WarmImage("daily_bonus.webp");
            WarmImage("daily_bonus_gray.webp");
            WarmImage("daily_bonus_bg.webp");
            WarmImage("days_main_bg.webp");
            WarmImage("btn_claim.webp");

            WarmImage("days_current_bg.webp");
            WarmImage("days_gold_bg.webp");
            WarmImage("days_gray_bg.webp");

            WarmImage("solicon.webp");
            // SETTINGS POPUP IMAGE WARMUP
            WarmImage("dailybonus_popup_bg.webp");
            WarmImage("settings_forground.webp");
            WarmImage("btn_exit.webp");

            WarmImage("help.webp");
            WarmImage("terms.webp");
            WarmImage("privacy.webp");
            WarmImage("support.webp");

            WarmImage("line_bg.webp");
            WarmImage("switch_btn_on.webp");
            WarmImage("switch_btn_off.webp");

            //     ForceLayoutPass(cashGame);
            //     ForceLayoutPass(offlinePage);
            //       ForceLayoutPass(playWithFriends);
            //     ForceLayoutPass(practicePage);
            //     ForceLayoutPass(friendsPage);

            if (profileInfo is BasePopup bpProfile && bpProfile.PopupContentContainer is VisualElement veProfile)
                ForceLayoutPass(veProfile);
            if (settings is BasePopup bpsettingsProfile && bpsettingsProfile.PopupContentContainer is VisualElement vesettingsProfile)
                ForceLayoutPass(vesettingsProfile);
         /*   if (editInfo is BasePopup bpeditInfoProfile && bpeditInfoProfile.PopupContentContainer is VisualElement veeditInfoProfile)
                ForceLayoutPass(veeditInfoProfile);
            if (helpDesk is BasePopup bphelpDeskProfile && bphelpDeskProfile.PopupContentContainer is VisualElement vehelpDeskProfile)
                ForceLayoutPass(vehelpDeskProfile);*/
            if (dailyBonus is BasePopup bpdailyBonusProfile && bpdailyBonusProfile.PopupContentContainer is VisualElement vedailyBonusProfile)
                ForceLayoutPass(vedailyBonusProfile);

        }
        public static void ForceLayoutPass(VisualElement page)
        {
            page.Handler?.DisconnectHandler();
            page.ToHandler(Application.Current.Handler.MauiContext);
            // Measure and layout off-screen
            page.Measure(width, height);
            page.Layout(new Rect(0, 0, width, height));
        }
        static void WarmImage(string file)
        {
            try
            {
                var img = new Image
                {
                    Source = ImageSource.FromFile(file)
                };

                // Force decode
                img.Measure(10, 10);
            }
            catch { }
        }
        public static void ForceLayoutPass(ContentPage page)
        {
            page.Handler?.DisconnectHandler();
            page.ToHandler(Application.Current.Handler.MauiContext);
            // Measure and layout off-screen
            page.Measure(width, height);
            page.Layout(new Rect(0, 0, width, height));
        }
        internal static void GoBack()
        {
            var existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();

            // Ensure there is at least one page to remove (i.e. the page before the current one).
            if (existingPages.Count > 1)
            {
                // Remove the page immediately below the current (top) page.
                ClientGlobalConstants.dashBoard.Navigation.RemovePage(existingPages[existingPages.Count - 1]);
                existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();
                if (existingPages.Count != 1)
                    ClientGlobalConstants.dashBoard.Navigation.RemovePage(existingPages[existingPages.Count - 1]);
            }
        }
        internal static void FlushOld()
        {
            // Retrieve a copy of the current navigation stack.
            var existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();

            // Ensure there is at least one page to remove (i.e. the page before the current one).
            if (existingPages.Count > 1)
            {
                // Remove the page immediately below the current (top) page.
                ClientGlobalConstants.dashBoard.Navigation.RemovePage(existingPages[existingPages.Count - 2]);
                existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();
                if (existingPages.Count != 2)
                    ClientGlobalConstants.dashBoard.Navigation.RemovePage(existingPages[existingPages.Count - 2]);
            }
        }

        internal static string NormalizeCoins(decimal val)
        {
            return NormalizeCoinsDecimal(val) + " LUDC";
        }

        internal static decimal NormalizeCoinsDecimal(decimal val)
        {
            return Math.Floor(val * 100) / 100;
        }
    }
}