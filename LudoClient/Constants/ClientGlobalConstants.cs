using CommunityToolkit.Maui.Views;
using LudoClient.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.Constants
{
    public static class ClientGlobalConstants
    {
        public static DashboardPage dashBoard;
        private static double width;
        private static double height;

        public static CashGame cashGame = new CashGame();
        public static FriendsPage friendsPage = new FriendsPage();
        public static OfflinePage offlinePage = new OfflinePage();
        public static PracticePage practicePage = new PracticePage();
        public static PlayWithFriends playWithFriends = new PlayWithFriends();

        public static EditInfo editInfo = new EditInfo();
        public static Settings settings = new Settings();
        public static HelpDesk helpDesk = new HelpDesk();
        public static DailyBonus dailyBonus = new DailyBonus();
        public static ProfileInfo profileInfo = new ProfileInfo();
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


            ForceLayoutPass(cashGame);
            ForceLayoutPass(offlinePage);
            ForceLayoutPass(playWithFriends);
            ForceLayoutPass(practicePage);
            ForceLayoutPass(friendsPage);

            ForceLayoutPass((VisualElement)(object)editInfo);
            ForceLayoutPass((VisualElement)(object)settings);
            ForceLayoutPass((VisualElement)(object)helpDesk);
            ForceLayoutPass((VisualElement)(object)dailyBonus);
            ForceLayoutPass((VisualElement)(object)profileInfo);
            
        }
        public static void ForceLayoutPass(VisualElement page)
        {
            // Measure and layout off-screen
            page.Measure(width, height);
            page.Layout(new Rect(0, 0, width, height));
        }

        public static void ForceLayoutPass(ContentPage page)
        {
            // Measure and layout off-screen
            page.Measure(width, height);
            page.Layout(new Rect(0, 0, width, height));
        }
    }
}