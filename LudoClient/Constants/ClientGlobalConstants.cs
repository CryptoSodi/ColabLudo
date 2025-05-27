using CommunityToolkit.Maui.Views;
using LudoClient.CoreEngine;
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
        public static HepticEngine hepticEngine = new HepticEngine();
        public static DashboardPage dashBoard;
        private static double width;
        private static double height;

        public static CashGame cashGame;
        public static FriendsPage friendsPage;
        public static OfflinePage offlinePage;
        public static PracticePage practicePage;
        public static PlayWithFriends playWithFriends;

        public static EditInfo editInfo;
        public static Settings settings;
        public static HelpDesk helpDesk;
        public static DailyBonus dailyBonus;
        public static ProfileInfo profileInfo;
        public static Results results;

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

            ForceLayoutPass(cashGame);
            ForceLayoutPass(offlinePage);
            ForceLayoutPass(playWithFriends);
            ForceLayoutPass(practicePage);
            ForceLayoutPass(friendsPage);

            if (profileInfo is BasePopup bpProfile && bpProfile.PopupContentContainer is VisualElement veProfile)
                ForceLayoutPass(veProfile);
            if (settings is BasePopup bpsettingsProfile && bpsettingsProfile.PopupContentContainer is VisualElement vesettingsProfile)
                ForceLayoutPass(vesettingsProfile);
            if (editInfo is BasePopup bpeditInfoProfile && bpeditInfoProfile.PopupContentContainer is VisualElement veeditInfoProfile)
                ForceLayoutPass(veeditInfoProfile);
            if (helpDesk is BasePopup bphelpDeskProfile && bphelpDeskProfile.PopupContentContainer is VisualElement vehelpDeskProfile)
                ForceLayoutPass(vehelpDeskProfile);
            if (dailyBonus is BasePopup bpdailyBonusProfile && bpdailyBonusProfile.PopupContentContainer is VisualElement vedailyBonusProfile)
                ForceLayoutPass(vedailyBonusProfile);
            profileInfo.loadValues();
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
    }
}