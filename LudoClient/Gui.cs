using LudoClient.ControlView;
using SharedCode.CoreEngine;

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
        public Token LockHome1;
        public Token LockHome2;
        public Token LockHome3;
        public Token LockHome4;

        public PlayerSeat red;
        public PlayerSeat green;
        public PlayerSeat yellow;
        public PlayerSeat blue;

        public Gui(Token red1, Token red2, Token red3, Token red4, Token gre1, Token gre2, Token gre3, Token gre4, Token blu1, Token blu2, Token blu3, Token blu4, Token yel1, Token yel2, Token yel3, Token yel4, Token LockHome1, Token LockHome2, Token LockHome3, Token LockHome4, PlayerSeat red, PlayerSeat green, PlayerSeat yellow, PlayerSeat blue)
        {
            this.red = red;
            this.green = green;
            this.yellow = yellow;
            this.blue = blue;

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
            this.LockHome1 = LockHome1;
            this.LockHome1.name = "LockHome1";
            this.LockHome2 = LockHome2;
            this.LockHome2.name = "LockHome2";
            this.LockHome3 = LockHome3;
            this.LockHome3.name = "LockHome3";
            this.LockHome4 = LockHome4;
            this.LockHome4.name = "LockHome4";
        }

        public Token getPieceToken(Piece piece)
        {
            // Compare the name of the piece with each token's name and return the matching token
            if (red1.name == piece.Name) return red1;
            if (red2.name == piece.Name) return red2;
            if (red3.name == piece.Name) return red3;
            if (red4.name == piece.Name) return red4;

            if (gre1.name == piece.Name) return gre1;
            if (gre2.name == piece.Name) return gre2;
            if (gre3.name == piece.Name) return gre3;
            if (gre4.name == piece.Name) return gre4;

            if (blu1.name == piece.Name) return blu1;
            if (blu2.name == piece.Name) return blu2;
            if (blu3.name == piece.Name) return blu3;
            if (blu4.name == piece.Name) return blu4;

            if (yel1.name == piece.Name) return yel1;
            if (yel2.name == piece.Name) return yel2;
            if (yel3.name == piece.Name) return yel3;
            if (yel4.name == piece.Name) return yel4;

            // Return null if no token matches the piece's name
            return null;
        }
    }
}