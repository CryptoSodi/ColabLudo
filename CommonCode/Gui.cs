
using static LudoClient.CoreEngine.Engine.Piece;

namespace LudoClient
{
    public class Gui
    {
        public Token red1;
        public Token red2;
        public Token red3;
        public Token red4;
        public Token gre1;
        public Token gre2;
        public Token gre3;
        public Token gre4;
        public Token blu1;
        public Token blu2;
        public Token blu3;
        public Token blu4;
        public Token yel1;
        public Token yel2;
        public Token yel3;
        public Token yel4;

        public PlayerSeat red;
        public PlayerSeat green;
        public PlayerSeat yellow;
        public PlayerSeat blue;

        public Gui(Token red1, Token red2, Token red3, Token red4, Token gre1, Token gre2, Token gre3, Token gre4, Token blu1, Token blu2, Token blu3, Token blu4, Token yel1, Token yel2, Token yel3, Token yel4,PlayerSeat red,PlayerSeat green,PlayerSeat yellow, PlayerSeat blue)
        {
            this.red = red;
            this.red.name = "red";
            this.green = green;
            this.green.name = "green";
            this.yellow = yellow;
            this.yellow.name = "yellow";
            this.blue = blue;
            this.blue.name = "blue";
            
            this.red1 = red1;
            this.red1.name = "red1";
            this.red2 = red2;
            this.red2.name = "red2";
            this.red3 = red3;
            this.red3.name = "red3";
            this.red4 = red4;
            this.red4.name = "red4";
            this.gre1 = gre1;
            this.gre1.name = "gre1";
            this.gre2 = gre2;
            this.gre2.name = "gre2";
            this.gre3 = gre3;
            this.gre3.name = "gre3";
            this.gre4 = gre4;
            this.gre4.name = "gre4";
            this.blu1 = blu1;
            this.blu1.name = "blu1";
            this.blu2 = blu2;
            this.blu2.name = "blu2";
            this.blu3 = blu3;
            this.blu3.name = "blu3";
            this.blu4 = blu4;
            this.blu4.name = "blu4";
            this.yel1 = yel1;
            this.yel1.name = "yel1";
            this.yel2 = yel2;
            this.yel2.name = "yel2";
            this.yel3 = yel3;
            this.yel3.name = "yel3";
            this.yel4 = yel4;
            this.yel4.name = "yel4";
        }
    }
}