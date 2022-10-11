namespace Chess
{
    public static class Piece
    {
        public const int None = 0;
        public const int King = 1;
        public const int Pawn = 2;
        public const int Knight = 3;
        public const int Bishop = 5;
        public const int Rook = 6;
        public const int Queen = 7;

        public const int White = 8;
        public const int Black = 16;

        private const int typeMask = 0b00111;
        private const int blackMask = 0b10000;
        private const int whiteMask = 0b01000;
        private const int colorMask = whiteMask | blackMask;

        public static bool IsColor(int piece, int color)
        {
            return (piece & colorMask) == color;
        }

        public static int Color(int piece)
        {
            return piece & colorMask;
        }

        public static int PieceType(int piece)
        {
            return piece & typeMask;
        }

        public static bool IsRookOrQueen(int piece)
        {
            return (piece & 0b110) == 0b110;
        }

        public static bool IsBishopOrQueen(int piece)
        {
            return (piece & 0b101) == 0b101;
        }

        public static bool IsSlidingPiece(int piece)
        {
            return (piece & 0b100) != 0;
        }
    }
}