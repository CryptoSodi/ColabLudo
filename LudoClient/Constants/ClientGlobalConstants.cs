using LudoClient.CoreEngine;
using LudoClient.Popups;
using LudoClient.SolanaWallet;
using Microsoft.Maui.Platform;
using System.Diagnostics;

namespace LudoClient.Constants
{
    public static class ClientGlobalConstants
    {
        public static LudoClient.SolanaWallet.WalletConnection WalletConnection { get; } = new LudoClient.SolanaWallet.WalletConnection();
        public static Stopwatch sw;   // START TIMER
        private static HepticEngine _hepticEngine;
        public static HepticEngine hepticEngine => _hepticEngine ??= Application.Current?.Handler?.MauiContext?.Services.GetService<HepticEngine>();
        public static DashboardPage dashBoard;
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
