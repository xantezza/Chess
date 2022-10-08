using System;
using System.Collections.Generic;

namespace Chess
{
    public static class PrecomputedMoveData
    {
        public static readonly int[] directionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };

        public static readonly int[][] numSquaresToEdge;

        public static readonly byte[][] knightMoves;
        public static readonly byte[][] kingMoves;

        public static readonly byte[][] pawnAttackDirections = {
            new byte[] { 4, 6 },
            new byte[] { 7, 5 }
        };

        public static readonly int[][] pawnAttacksWhite;
        public static readonly int[][] pawnAttacksBlack;
        public static readonly int[] directionLookup;

        public static readonly ulong[] kingAttackBitboards;
        public static readonly ulong[] knightAttackBitboards;
        public static readonly ulong[][] pawnAttackBitboards;

        public static readonly ulong[] rookMoves;
        public static readonly ulong[] bishopMoves;
        public static readonly ulong[] queenMoves;

        public static int[,] orthogonalDistance;
        public static int[,] kingDistance;
        public static int[] centreManhattanDistance;

        public static int NumRookMovesToReachSquare(int startSquare, int targetSquare)
        {
            return orthogonalDistance[startSquare, targetSquare];
        }

        public static int NumKingMovesToReachSquare(int startSquare, int targetSquare)
        {
            return kingDistance[startSquare, targetSquare];
        }

        static PrecomputedMoveData()
        {
            pawnAttacksWhite = new int[64][];
            pawnAttacksBlack = new int[64][];
            numSquaresToEdge = new int[8][];
            knightMoves = new byte[64][];
            kingMoves = new byte[64][];
            numSquaresToEdge = new int[64][];

            rookMoves = new ulong[64];
            bishopMoves = new ulong[64];
            queenMoves = new ulong[64];

            int[] allKnightJumps = { 15, 17, -17, -15, 10, -6, 6, -10 };
            knightAttackBitboards = new ulong[64];
            kingAttackBitboards = new ulong[64];
            pawnAttackBitboards = new ulong[64][];

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {

                int y = squareIndex / 8;
                int x = squareIndex - y * 8;

                int north = 7 - y;
                int south = y;
                int west = x;
                int east = 7 - x;
                numSquaresToEdge[squareIndex] = new int[8];
                numSquaresToEdge[squareIndex][0] = north;
                numSquaresToEdge[squareIndex][1] = south;
                numSquaresToEdge[squareIndex][2] = west;
                numSquaresToEdge[squareIndex][3] = east;
                numSquaresToEdge[squareIndex][4] = System.Math.Min(north, west);
                numSquaresToEdge[squareIndex][5] = System.Math.Min(south, east);
                numSquaresToEdge[squareIndex][6] = System.Math.Min(north, east);
                numSquaresToEdge[squareIndex][7] = System.Math.Min(south, west);

                var legalKnightJumps = new List<byte>();
                ulong knightBitboard = 0;
                foreach (int knightJumpDelta in allKnightJumps)
                {
                    int knightJumpSquare = squareIndex + knightJumpDelta;
                    if (knightJumpSquare >= 0 && knightJumpSquare < 64)
                    {
                        int knightSquareY = knightJumpSquare / 8;
                        int knightSquareX = knightJumpSquare - knightSquareY * 8;

                        int maxCoordMoveDst = System.Math.Max(System.Math.Abs(x - knightSquareX), System.Math.Abs(y - knightSquareY));
                        if (maxCoordMoveDst == 2)
                        {
                            legalKnightJumps.Add((byte)knightJumpSquare);
                            knightBitboard |= 1ul << knightJumpSquare;
                        }
                    }
                }
                knightMoves[squareIndex] = legalKnightJumps.ToArray();
                knightAttackBitboards[squareIndex] = knightBitboard;

                var legalKingMoves = new List<byte>();
                foreach (int kingMoveDelta in directionOffsets)
                {
                    int kingMoveSquare = squareIndex + kingMoveDelta;
                    if (kingMoveSquare >= 0 && kingMoveSquare < 64)
                    {
                        int kingSquareY = kingMoveSquare / 8;
                        int kingSquareX = kingMoveSquare - kingSquareY * 8;
                        int maxCoordMoveDst = System.Math.Max(System.Math.Abs(x - kingSquareX), System.Math.Abs(y - kingSquareY));
                        if (maxCoordMoveDst == 1)
                        {
                            legalKingMoves.Add((byte)kingMoveSquare);
                            kingAttackBitboards[squareIndex] |= 1ul << kingMoveSquare;
                        }
                    }
                }
                kingMoves[squareIndex] = legalKingMoves.ToArray();

                List<int> pawnCapturesWhite = new List<int>();
                List<int> pawnCapturesBlack = new List<int>();
                pawnAttackBitboards[squareIndex] = new ulong[2];
                if (x > 0)
                {
                    if (y < 7)
                    {
                        pawnCapturesWhite.Add(squareIndex + 7);
                        pawnAttackBitboards[squareIndex][Board.WhiteIndex] |= 1ul << (squareIndex + 7);
                    }
                    if (y > 0)
                    {
                        pawnCapturesBlack.Add(squareIndex - 9);
                        pawnAttackBitboards[squareIndex][Board.BlackIndex] |= 1ul << (squareIndex - 9);
                    }
                }
                if (x < 7)
                {
                    if (y < 7)
                    {
                        pawnCapturesWhite.Add(squareIndex + 9);
                        pawnAttackBitboards[squareIndex][Board.WhiteIndex] |= 1ul << (squareIndex + 9);
                    }
                    if (y > 0)
                    {
                        pawnCapturesBlack.Add(squareIndex - 7);
                        pawnAttackBitboards[squareIndex][Board.BlackIndex] |= 1ul << (squareIndex - 7);
                    }
                }
                pawnAttacksWhite[squareIndex] = pawnCapturesWhite.ToArray();
                pawnAttacksBlack[squareIndex] = pawnCapturesBlack.ToArray();

                for (int directionIndex = 0; directionIndex < 4; directionIndex++)
                {
                    int currentDirOffset = directionOffsets[directionIndex];
                    for (int n = 0; n < numSquaresToEdge[squareIndex][directionIndex]; n++)
                    {
                        int targetSquare = squareIndex + currentDirOffset * (n + 1);
                        rookMoves[squareIndex] |= 1ul << targetSquare;
                    }
                }

                for (int directionIndex = 4; directionIndex < 8; directionIndex++)
                {
                    int currentDirOffset = directionOffsets[directionIndex];
                    for (int n = 0; n < numSquaresToEdge[squareIndex][directionIndex]; n++)
                    {
                        int targetSquare = squareIndex + currentDirOffset * (n + 1);
                        bishopMoves[squareIndex] |= 1ul << targetSquare;
                    }
                }
                queenMoves[squareIndex] = rookMoves[squareIndex] | bishopMoves[squareIndex];
            }

            directionLookup = new int[127];
            for (int i = 0; i < 127; i++)
            {
                int offset = i - 63;
                int absOffset = System.Math.Abs(offset);
                int absDir = 1;
                if (absOffset % 9 == 0)
                {
                    absDir = 9;
                }
                else if (absOffset % 8 == 0)
                {
                    absDir = 8;
                }
                else if (absOffset % 7 == 0)
                {
                    absDir = 7;
                }

                directionLookup[i] = absDir * System.Math.Sign(offset);
            }

            orthogonalDistance = new int[64, 64];
            kingDistance = new int[64, 64];
            centreManhattanDistance = new int[64];
            for (int squareA = 0; squareA < 64; squareA++)
            {
                Coord coordA = BoardRepresentation.CoordFromIndex(squareA);
                int fileDstFromCentre = Math.Max(3 - coordA.fileIndex, coordA.fileIndex - 4);
                int rankDstFromCentre = Math.Max(3 - coordA.rankIndex, coordA.rankIndex - 4);
                centreManhattanDistance[squareA] = fileDstFromCentre + rankDstFromCentre;

                for (int squareB = 0; squareB < 64; squareB++)
                {

                    Coord coordB = BoardRepresentation.CoordFromIndex(squareB);
                    int rankDistance = Math.Abs(coordA.rankIndex - coordB.rankIndex);
                    int fileDistance = Math.Abs(coordA.fileIndex - coordB.fileIndex);
                    orthogonalDistance[squareA, squareB] = fileDistance + rankDistance;
                    kingDistance[squareA, squareB] = Math.Max(fileDistance, rankDistance);
                }
            }
        }
    }
}