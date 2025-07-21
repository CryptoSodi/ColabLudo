using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.Constants
{
    public static class Skins
    {
        public enum SkinTypes
        {
            [Description("Default Skin")]
            DefaultSkin = 0,
            [Description("Adatiya Skin")]
            Adatiya = 1
        }
        public static SkinTypes CurrentSkin { get; set; } = SkinTypes.DefaultSkin;
        private static string CurrentSkinType => CurrentSkin == SkinTypes.DefaultSkin ? "" : "_a";
        //Spacing for the stacklayout in the game setup pages
        public static double StackLayoutSpacing => CurrentSkin == SkinTypes.DefaultSkin ? 15 : 20;
        //BACKGROUNDS
        public static string Background => $"background{CurrentSkinType}.webp";
        public static string Settings_Background => $"game_setting_bg{CurrentSkinType}.webp";
        public static string Background_Waitingroom => $"background_waitingroom{CurrentSkinType}.webp";
        public static string Background_Controlbox => $"controlbox{CurrentSkinType}.webp";
        public static string Background_Controlbox_Small => $"controlbox_small{CurrentSkinType}.webp";
        
        //GRIDS
        public static string DashboardMainGridDefinition => CurrentSkin == SkinTypes.DefaultSkin ? ".8*,1.6*,1*,1*,1*,1*,.8*" : ".8*,2*,.1*,1.3*,1.3*,1*,.7*";
        public static string OfflineGridDefinition => CurrentSkin == SkinTypes.DefaultSkin ? "1*" : "3*,1*";

        //COMMON BUTTONS
        public static string PlayBTN => $"btn_play_large{CurrentSkinType}.webp";
        public static string PasteBTN => $"btn_paste{CurrentSkinType}.webp";
        //DASHBOARD
        public static string Logo => $"logo{CurrentSkinType}.webp";
        public static string Offline => $"offline{CurrentSkinType}.webp";
        public static string Cash => $"cashgame{CurrentSkinType}.webp";
        public static string Play => $"playwithfriends{CurrentSkinType}.webp";
        public static string Practice => $"practice{CurrentSkinType}.webp";
        public static string Tournament => $"tournament{CurrentSkinType}.webp";
        public static string Cash_Gray => $"cashgame{CurrentSkinType}_gray.webp";
        public static string Play_Gray => $"playwithfriends{CurrentSkinType}_gray.webp";
        public static string Practice_Gray => $"practice{CurrentSkinType}_gray.webp";
        public static string Tournament_Gray => $"tournament{CurrentSkinType}_gray.webp";

        public static string DailyBonus => $"daily_bonus{CurrentSkinType}.webp";
        public static string DailyBonus_Gray => $"daily_bonus{CurrentSkinType}_gray.webp";
        //OFFLINE
        public static string Title_Offline => $"round_offline{CurrentSkinType}.webp";
        //PRACTICE
        public static string Title_Practice => $"round_practice{CurrentSkinType}.webp";
        //CASH GAME
        public static string Title_Cash => $"round_cashgames{CurrentSkinType}.webp";
        //PLAY WITH FRIENDS
        public static string Title_PlayWithFriends => $"round_cashgames{CurrentSkinType}.webp";
        public static string CreateBTN => $"btn_create{CurrentSkinType}.webp";
        public static string JoinBTN => $"btn_join_large{CurrentSkinType}.webp";
        public static string MinusBTN => $"btn_minus_large{CurrentSkinType}.webp";
        public static string PlusBTN => $"btn_plus_large{CurrentSkinType}.webp";
        //WAITING ROOM
        public static string VS => $"vs{CurrentSkinType}.webp";
        //GAME BOARD
        public static string GameBoard => $"ludoboard.webp";
        public static string RedToken => $"red_token_large.webp";
        public static string GreenToken => $"green_token_large.webp";
        public static string YellowToken => $"yellow_token_large.webp";
        public static string BlueToken => $"blue_token_large.webp";

        public static string LockHome => $"ludoLockHome.webp";
    }
}