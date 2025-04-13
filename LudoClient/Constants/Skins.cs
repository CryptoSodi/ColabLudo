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
        public static string Background => $"background{CurrentSkinType}.png";
        public static string Settings_Background => $"game_setting_bg{CurrentSkinType}.png";
        public static string Background_Waitingroom => $"background_waitingroom{CurrentSkinType}.png";
        public static string Background_Controlbox => $"controlbox{CurrentSkinType}.png";
        public static string Background_Controlbox_Small => $"controlbox_small{CurrentSkinType}.png";
        
        //GRIDS
        public static string DashboardMainGridDefinition => CurrentSkin == SkinTypes.DefaultSkin ? ".8*,1.6*,1*,1*,1*,1*,.8*" : ".8*,2*,.1*,1.3*,1.3*,1*,.7*";
        public static string OfflineGridDefinition => CurrentSkin == SkinTypes.DefaultSkin ? "1*" : "3*,1*";

        //COMMON BUTTONS
        public static string PlayBTN => $"btn_play_large{CurrentSkinType}.png";
        public static string PasteBTN => $"btn_paste{CurrentSkinType}.png";
        //DASHBOARD
        public static string Logo => $"logo{CurrentSkinType}.png";
        public static string Offline => $"offline{CurrentSkinType}.png";
        public static string Cash => $"cashgame{CurrentSkinType}.png";
        public static string Play => $"playwithfriends{CurrentSkinType}.png";
        public static string Practice => $"practice{CurrentSkinType}.png";
        public static string Tournament => $"tournament{CurrentSkinType}.png";
        public static string DailyBonus => $"daily_bonus{CurrentSkinType}.png";
        //OFFLINE
        public static string Title_Offline => $"round_offline{CurrentSkinType}.png";
        //PRACTICE
        public static string Title_Practice => $"round_practice{CurrentSkinType}.png";
        //CASH GAME
        public static string Title_Cash => $"round_cashgames{CurrentSkinType}.png";
        //PLAY WITH FRIENDS
        public static string Title_PlayWithFriends => $"round_cashgames{CurrentSkinType}.png";
        public static string CreateBTN => $"btn_create{CurrentSkinType}.png";
        public static string JoinBTN => $"btn_join_large{CurrentSkinType}.png";
        public static string MinusBTN => $"btn_minus_large{CurrentSkinType}.png";
        public static string PlusBTN => $"btn_plus_large{CurrentSkinType}.png";
        //WAITING ROOM
        public static string VS => $"vs{CurrentSkinType}.png";
        //GAME BOARD
        public static string GameBoard => $"board_game.png";
        public static string RedToken => $"red_token_large.png";
        public static string GreenToken => $"green_token_large.png";
        public static string YellowToken => $"yellow_token_large.png";
        public static string BlueToken => $"blue_token_large.png";
        public static string LockHome => $"ludoLockHome.png";
    }
}