namespace Chess
{
    public readonly struct Move
    {
        public readonly struct Flag
        {
            public const int None = 0;
            public const int EnPassantCapture = 1;
            public const int Castling = 2;
            public const int PromoteToQueen = 3;
            public const int PromoteToKnight = 4;
            public const int PromoteToRook = 5;
            public const int PromoteToBishop = 6;
            public const int PawnTwoForward = 7;
        }

        private readonly ushort moveValue;

        private const ushort startSquareMask = 0b0000000000111111;
        private const ushort targetSquareMask = 0b0000111111000000;
        private const ushort flagMask = 0b1111000000000000;

        public Move(ushort moveValue)
        {
            this.moveValue = moveValue;
        }

        public Move(int startSquare, int targetSquare)
        {
            moveValue = (ushort)(startSquare | targetSquare << 6);
        }

        public Move(int startSquare, int targetSquare, int flag)
        {
            moveValue = (ushort)(startSquare | targetSquare << 6 | flag << 12);
        }

        public int StartSquare
        {
            get
            {
                return moveValue & startSquareMask;
            }
        }

        public int TargetSquare
        {
            get
            {
                return (moveValue & targetSquareMask) >> 6;
            }
        }

        public bool IsPromotion
        {
            get
            {
                int flag = MoveFlag;
                return flag == Flag.PromoteToQueen || flag == Flag.PromoteToRook || flag == Flag.PromoteToKnight || flag == Flag.PromoteToBishop;
            }
        }

        public int MoveFlag
        {
            get
            {
                return moveValue >> 12;
            }
        }

        public int PromotionPieceType
        {
            get
            {
                switch (MoveFlag)
                {
                    case Flag.PromoteToRook:
                        return Piece.Rook;
                    case Flag.PromoteToKnight:
                        return Piece.Knight;
                    case Flag.PromoteToBishop:
                        return Piece.Bishop;
                    case Flag.PromoteToQueen:
                        return Piece.Queen;
                    default:
                        return Piece.None;
                }
            }
        }

        public static Move InvalidMove
        {
            get
            {
                return new Move(0);
            }
        }

        public static bool SameMove(Move a, Move b)
        {
            return a.moveValue == b.moveValue;
        }

        public ushort Value
        {
            get
            {
                return moveValue;
            }
        }

        public bool IsInvalid
        {
            get
            {
                return moveValue == 0;
            }
        }

        public string Name
        {
            get
            {
                return string.Format("{0}-{1}", BoardRepresentation.SquareNameFromIndex(StartSquare), BoardRepresentation.SquareNameFromIndex(TargetSquare));
            }
        }
    }
}
