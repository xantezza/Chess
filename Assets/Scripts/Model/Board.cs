using System.Collections.Generic;

namespace Chess
{
    public class Board
    {
        public const int WhiteIndex = 0;
        public const int BlackIndex = 1;

        public int[] Square;

        public bool WhiteToMove;
        public int ColorToMove;
        public int OpponentColor;
        public int ColorToMoveIndex;

        public uint currentGameState;

        public int plyCount;
        public int fiftyMoveCounter;

        public int[] KingSquare;

        public PieceList[] rooks;
        public PieceList[] bishops;
        public PieceList[] queens;
        public PieceList[] knights;
        public PieceList[] pawns;

        private Stack<uint> gameStateHistory;
        private PieceList[] allPieceLists;

        private const uint whiteCastleKingsideMask = 0b1111111111111110;
        private const uint whiteCastleQueensideMask = 0b1111111111111101;
        private const uint blackCastleKingsideMask = 0b1111111111111011;
        private const uint blackCastleQueensideMask = 0b1111111111110111;

        private const uint whiteCastleMask = whiteCastleKingsideMask & whiteCastleQueensideMask;
        private const uint blackCastleMask = blackCastleKingsideMask & blackCastleQueensideMask;

        private PieceList GetPieceList(int pieceType, int colorIndex)
        {
            return allPieceLists[colorIndex * 8 + pieceType];
        }

        public void MakeMove(Move move, bool inSearch = false)
        {
            uint oldEnPassantFile = (currentGameState >> 4) & 15;
            uint originalCastleState = currentGameState & 15;
            uint newCastleState = originalCastleState;
            currentGameState = 0;

            int opponentColorIndex = 1 - ColorToMoveIndex;
            int moveFrom = move.StartSquare;
            int moveTo = move.TargetSquare;

            int capturedPieceType = Piece.PieceType(Square[moveTo]);
            int movePiece = Square[moveFrom];
            int movePieceType = Piece.PieceType(movePiece);

            int moveFlag = move.MoveFlag;
            bool isPromotion = move.IsPromotion;
            bool isEnPassant = moveFlag == Move.Flag.EnPassantCapture;

            currentGameState |= (ushort)(capturedPieceType << 8);
            if (capturedPieceType != 0 && !isEnPassant)
            {
                GetPieceList(capturedPieceType, opponentColorIndex).RemovePieceAtSquare(moveTo);
            }

            if (movePieceType == Piece.King)
            {
                KingSquare[ColorToMoveIndex] = moveTo;
                newCastleState &= (WhiteToMove) ? whiteCastleMask : blackCastleMask;
            }
            else
            {
                GetPieceList(movePieceType, ColorToMoveIndex).MovePiece(moveFrom, moveTo);
            }

            int pieceOnTargetSquare = movePiece;

            if (isPromotion)
            {
                int promoteType = 0;
                switch (moveFlag)
                {
                    case Move.Flag.PromoteToQueen:
                        promoteType = Piece.Queen;
                        queens[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToRook:
                        promoteType = Piece.Rook;
                        rooks[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToBishop:
                        promoteType = Piece.Bishop;
                        bishops[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToKnight:
                        promoteType = Piece.Knight;
                        knights[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;

                }
                pieceOnTargetSquare = promoteType | ColorToMove;
                pawns[ColorToMoveIndex].RemovePieceAtSquare(moveTo);
            }
            else
            {
                switch (moveFlag)
                {
                    case Move.Flag.EnPassantCapture:
                        int epPawnSquare = moveTo + ((ColorToMove == Piece.White) ? -8 : 8);
                        currentGameState |= (ushort)(Square[epPawnSquare] << 8);
                        Square[epPawnSquare] = 0;
                        pawns[opponentColorIndex].RemovePieceAtSquare(epPawnSquare);
                        break;
                    case Move.Flag.Castling:
                        bool kingside = moveTo == BoardRepresentation.g1 || moveTo == BoardRepresentation.g8;
                        int castlingRookFromIndex = (kingside) ? moveTo + 1 : moveTo - 2;
                        int castlingRookToIndex = (kingside) ? moveTo - 1 : moveTo + 1;

                        Square[castlingRookFromIndex] = Piece.None;
                        Square[castlingRookToIndex] = Piece.Rook | ColorToMove;

                        rooks[ColorToMoveIndex].MovePiece(castlingRookFromIndex, castlingRookToIndex);
                        break;
                }
            }

            Square[moveTo] = pieceOnTargetSquare;
            Square[moveFrom] = 0;

            if (moveFlag == Move.Flag.PawnTwoForward)
            {
                int file = BoardRepresentation.FileIndex(moveFrom) + 1;
                currentGameState |= (ushort)(file << 4);
            }

            if (originalCastleState != 0)
            {
                if (moveTo == BoardRepresentation.h1 || moveFrom == BoardRepresentation.h1)
                {
                    newCastleState &= whiteCastleKingsideMask;
                }
                else if (moveTo == BoardRepresentation.a1 || moveFrom == BoardRepresentation.a1)
                {
                    newCastleState &= whiteCastleQueensideMask;
                }
                if (moveTo == BoardRepresentation.h8 || moveFrom == BoardRepresentation.h8)
                {
                    newCastleState &= blackCastleKingsideMask;
                }
                else if (moveTo == BoardRepresentation.a8 || moveFrom == BoardRepresentation.a8)
                {
                    newCastleState &= blackCastleQueensideMask;
                }
            }

            currentGameState |= newCastleState;
            currentGameState |= (uint)fiftyMoveCounter << 14;
            gameStateHistory.Push(currentGameState);

            WhiteToMove = !WhiteToMove;
            ColorToMove = (WhiteToMove) ? Piece.White : Piece.Black;
            OpponentColor = (WhiteToMove) ? Piece.Black : Piece.White;
            ColorToMoveIndex = 1 - ColorToMoveIndex;
            plyCount++;
            fiftyMoveCounter++;

            if (!inSearch)
            {
                if (movePieceType == Piece.Pawn || capturedPieceType != Piece.None)
                {
                    fiftyMoveCounter = 0;
                }
            }
        }

        public void LoadStartPosition()
        {
            LoadPosition(FenUtility.startFen);
        }

        public void LoadPosition(string fen)
        {
            Initialize();
            var loadedPosition = FenUtility.PositionFromFen(fen);

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                int piece = loadedPosition.squares[squareIndex];
                Square[squareIndex] = piece;

                if (piece != Piece.None)
                {
                    int pieceType = Piece.PieceType(piece);
                    int pieceColorIndex = (Piece.IsColor(piece, Piece.White)) ? WhiteIndex : BlackIndex;
                    if (Piece.IsSlidingPiece(piece))
                    {
                        if (pieceType == Piece.Queen)
                        {
                            queens[pieceColorIndex].AddPieceAtSquare(squareIndex);
                        }
                        else if (pieceType == Piece.Rook)
                        {
                            rooks[pieceColorIndex].AddPieceAtSquare(squareIndex);
                        }
                        else if (pieceType == Piece.Bishop)
                        {
                            bishops[pieceColorIndex].AddPieceAtSquare(squareIndex);
                        }
                    }
                    else if (pieceType == Piece.Knight)
                    {
                        knights[pieceColorIndex].AddPieceAtSquare(squareIndex);
                    }
                    else if (pieceType == Piece.Pawn)
                    {
                        pawns[pieceColorIndex].AddPieceAtSquare(squareIndex);
                    }
                    else if (pieceType == Piece.King)
                    {
                        KingSquare[pieceColorIndex] = squareIndex;
                    }
                }
            }

            WhiteToMove = loadedPosition.whiteToMove;
            ColorToMove = (WhiteToMove) ? Piece.White : Piece.Black;
            OpponentColor = (WhiteToMove) ? Piece.Black : Piece.White;
            ColorToMoveIndex = (WhiteToMove) ? 0 : 1;

            int whiteCastle = ((loadedPosition.whiteCastleKingside) ? 1 << 0 : 0) | ((loadedPosition.whiteCastleQueenside) ? 1 << 1 : 0);
            int blackCastle = ((loadedPosition.blackCastleKingside) ? 1 << 2 : 0) | ((loadedPosition.blackCastleQueenside) ? 1 << 3 : 0);
            int epState = loadedPosition.epFile << 4;
            ushort initialGameState = (ushort)(whiteCastle | blackCastle | epState);
            gameStateHistory.Push(initialGameState);
            currentGameState = initialGameState;
            plyCount = loadedPosition.plyCount;
        }

        private void Initialize()
        {
            Square = new int[64];
            KingSquare = new int[2];

            gameStateHistory = new Stack<uint>();
            plyCount = 0;
            fiftyMoveCounter = 0;

            knights = new PieceList[] { new PieceList(10), new PieceList(10) };
            pawns = new PieceList[] { new PieceList(8), new PieceList(8) };
            rooks = new PieceList[] { new PieceList(10), new PieceList(10) };
            bishops = new PieceList[] { new PieceList(10), new PieceList(10) };
            queens = new PieceList[] { new PieceList(9), new PieceList(9) };
            PieceList emptyList = new PieceList(0);
            allPieceLists = new PieceList[] {
                emptyList,
                emptyList,
                pawns[WhiteIndex],
                knights[WhiteIndex],
                emptyList,
                bishops[WhiteIndex],
                rooks[WhiteIndex],
                queens[WhiteIndex],
                emptyList,
                emptyList,
                pawns[BlackIndex],
                knights[BlackIndex],
                emptyList,
                bishops[BlackIndex],
                rooks[BlackIndex],
                queens[BlackIndex],
            };
        }
    }
}