using System.Collections;
using UnityEngine;

namespace Chess
{
    public class BoardView : MonoBehaviour
    {
        public PiecesTheme pieceTheme;
        public BoardTheme boardTheme;

        public bool showLegalMoves;

        public bool whiteIsBottom = true;

        private SpriteRenderer[,] squareRenderers;
        private SpriteRenderer[,] squarePieceRenderers;
        private Move lastMadeMove;
        private MoveGenerator moveGenerator;

        private void Awake()
        {
            moveGenerator = new MoveGenerator();
            CreateBoardUI();
        }

        public void HighlightLegalMoves(Board board, Coord fromSquare)
        {
            if (showLegalMoves)
            {

                var moves = moveGenerator.GenerateMoves(board);

                for (int i = 0; i < moves.Count; i++)
                {
                    Move move = moves[i];
                    if (move.StartSquare == BoardRepresentation.IndexFromCoord(fromSquare))
                    {
                        Coord coord = BoardRepresentation.CoordFromIndex(move.TargetSquare);
                        SetSquareColor(coord, boardTheme.lightSquares.legal, boardTheme.darkSquares.legal);
                    }
                }
            }
        }

        public void DragPiece(Coord pieceCoord, Vector2 mousePos)
        {
            squarePieceRenderers[pieceCoord.fileIndex, pieceCoord.rankIndex].transform.position = new Vector3(mousePos.x, mousePos.y, 0);
        }

        public void ResetPiecePosition(Coord pieceCoord)
        {
            Vector3 pos = PositionFromCoord(pieceCoord.fileIndex, pieceCoord.rankIndex, 0);
            squarePieceRenderers[pieceCoord.fileIndex, pieceCoord.rankIndex].transform.position = pos;
        }

        public void SelectSquare(Coord coord)
        {
            SetSquareColor(coord, boardTheme.lightSquares.selected, boardTheme.darkSquares.selected);
        }

        public void DeselectSquare(Coord coord)
        {
            ResetSquareColors();
        }

        public bool TryGetSquareUnderMouse(Vector2 mouseWorld, out Coord selectedCoord)
        {
            int file = (int)(mouseWorld.x + 4);
            int rank = (int)(mouseWorld.y + 4);
            if (!whiteIsBottom)
            {
                file = 7 - file;
                rank = 7 - rank;
            }
            selectedCoord = new Coord(file, rank);
            return file >= 0 && file < 8 && rank >= 0 && rank < 8;
        }

        public void UpdatePosition(Board board)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    Coord coord = new Coord(file, rank);
                    int piece = board.Square[BoardRepresentation.IndexFromCoord(coord.fileIndex, coord.rankIndex)];
                    squarePieceRenderers[file, rank].sprite = pieceTheme.GetPieceSprite(piece);
                    squarePieceRenderers[file, rank].transform.position = PositionFromCoord(file, rank, 0);
                }
            }
        }

        public void OnMoveMade(Board board, Move move, bool animate = false)
        {
            lastMadeMove = move;
            if (animate)
            {
                StartCoroutine(AnimateMove(move, board));
            }
            else
            {
                UpdatePosition(board);
                ResetSquareColors();
            }
        }

        private IEnumerator AnimateMove(Move move, Board board)
        {
            float t = 0;
            const float moveAnimDuration = 0.15f;
            Coord startCoord = BoardRepresentation.CoordFromIndex(move.StartSquare);
            Coord targetCoord = BoardRepresentation.CoordFromIndex(move.TargetSquare);
            Transform pieceT = squarePieceRenderers[startCoord.fileIndex, startCoord.rankIndex].transform;
            Vector3 startPos = PositionFromCoord(startCoord);
            Vector3 targetPos = PositionFromCoord(targetCoord);
            SetSquareColor(BoardRepresentation.CoordFromIndex(move.StartSquare), boardTheme.lightSquares.moveFromHighlight, boardTheme.darkSquares.moveFromHighlight);

            while (t <= 1)
            {
                yield return null;
                t += Time.deltaTime * 1 / moveAnimDuration;
                pieceT.position = Vector3.Lerp(startPos, targetPos, t);
            }
            UpdatePosition(board);
            ResetSquareColors();
            pieceT.position = startPos;
        }

        private void HighlightMove(Move move)
        {
            SetSquareColor(BoardRepresentation.CoordFromIndex(move.StartSquare), boardTheme.lightSquares.moveFromHighlight, boardTheme.darkSquares.moveFromHighlight);
            SetSquareColor(BoardRepresentation.CoordFromIndex(move.TargetSquare), boardTheme.lightSquares.moveToHighlight, boardTheme.darkSquares.moveToHighlight);
        }

        private void CreateBoardUI()
        {
            squareRenderers = new SpriteRenderer[8, 8];
            squarePieceRenderers = new SpriteRenderer[8, 8];

            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    SpriteRenderer square = new GameObject("Square").AddComponent<SpriteRenderer>();
                    square.sprite = boardTheme.boardTile;
                    square.drawMode = SpriteDrawMode.Sliced;
                    square.size = Vector2.one;
                    square.name = BoardRepresentation.SquareNameFromCoordinate(file, rank);
                    squareRenderers[file, rank] = square;

                    RectTransform squareTransform = square.gameObject.AddComponent<RectTransform>();
                    squareTransform.SetParent(transform);
                    squareTransform.anchoredPosition = PositionFromCoord(file, rank, 0);
                    squareTransform.sizeDelta = Vector2.one * 100f;

                    SpriteRenderer pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
                    pieceRenderer.sortingOrder = 2;
                    squarePieceRenderers[file, rank] = pieceRenderer;

                    Transform pieceTransform = pieceRenderer.transform;
                    pieceTransform.SetParent(squareTransform, true);
                    pieceTransform.localPosition = Vector3.zero;
                    pieceTransform.localScale = Vector2.one * 4;

                }
            }

            ResetSquarePositions();
            ResetSquareColors();
        }

        private void ResetSquarePositions()
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    squareRenderers[file, rank].transform.position = PositionFromCoord(file, rank, 0);
                    squarePieceRenderers[file, rank].transform.position = PositionFromCoord(file, rank, 0);
                }
            }

            if (!lastMadeMove.IsInvalid)
            {
                HighlightMove(lastMadeMove);
            }
        }

        public void SetPerspective(bool whitePOV)
        {
            whiteIsBottom = whitePOV;
            ResetSquarePositions();
        }

        public void ResetSquareColors(bool highlight = true)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    SetSquareColor(new Coord(file, rank), boardTheme.lightSquares.normal, boardTheme.darkSquares.normal);
                }
            }
            if (highlight)
            {
                if (!lastMadeMove.IsInvalid)
                {
                    HighlightMove(lastMadeMove);
                }
            }
        }

        private void SetSquareColor(Coord square, Color lightCol, Color darkCol)
        {
            squareRenderers[square.fileIndex, square.rankIndex].color = (square.IsLightSquare()) ? lightCol : darkCol;
        }

        public Vector3 PositionFromCoord(int file, int rank, float depth = 0)
        {
            if (whiteIsBottom)
            {
                return new Vector3(-3.5f + file, -3.5f + rank, depth);
            }
            return new Vector3(-3.5f + 7 - file, 7 - rank - 3.5f, depth);
        }

        public Vector3 PositionFromCoord(Coord coord, float depth = 0)
        {
            return PositionFromCoord(coord.fileIndex, coord.rankIndex, depth);
        }
    }
}