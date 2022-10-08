using UnityEngine;

namespace Chess
{
    public class Player
    {
        public event System.Action<Move> OnMoveChosen;

        public enum InputState
        {
            None,
            PieceSelected,
            DraggingPiece
        }

        private InputState currentState;

        private BoardView boardView;
        private Camera camera;
        private Coord selectedPieceSquare;
        private Board board;


        public Player(Board board)
        {
            boardView = GameObject.FindObjectOfType<BoardView>();
            camera = Camera.main;
            this.board = board;
        }

        public void Update()
        {
            HandleInput();
        }

        private void ChoseMove(Move move)
        {
            OnMoveChosen?.Invoke(move);
        }

        private void HandleInput()
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);

            if (currentState == InputState.None)
            {
                HandlePieceSelection(mousePos);
            }
            else if (currentState == InputState.DraggingPiece)
            {
                HandleDragMovement(mousePos);
            }
            else if (currentState == InputState.PieceSelected)
            {
                HandlePointAndClickMovement(mousePos);
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelPieceSelection();
            }
        }

        private void HandlePointAndClickMovement(Vector2 mousePos)
        {
            if (Input.GetMouseButton(0))
            {
                HandlePiecePlacement(mousePos);
            }
        }

        private void HandleDragMovement(Vector2 mousePos)
        {
            boardView.DragPiece(selectedPieceSquare, mousePos);

            if (Input.GetMouseButtonUp(0))
            {
                HandlePiecePlacement(mousePos);
            }
        }

        private void HandlePiecePlacement(Vector2 mousePos)
        {
            Coord targetSquare;
            if (boardView.TryGetSquareUnderMouse(mousePos, out targetSquare))
            {
                if (targetSquare.Equals(selectedPieceSquare))
                {
                    boardView.ResetPiecePosition(selectedPieceSquare);
                    if (currentState == InputState.DraggingPiece)
                    {
                        currentState = InputState.PieceSelected;
                    }
                    else
                    {
                        currentState = InputState.None;
                        boardView.DeselectSquare(selectedPieceSquare);
                    }
                }
                else
                {
                    int targetIndex = BoardRepresentation.IndexFromCoord(targetSquare.fileIndex, targetSquare.rankIndex);
                    if (Piece.IsColor(board.Square[targetIndex], board.ColorToMove) && board.Square[targetIndex] != 0)
                    {
                        CancelPieceSelection();
                        HandlePieceSelection(mousePos);
                    }
                    else
                    {
                        TryMakeMove(selectedPieceSquare, targetSquare);
                    }
                }
            }
            else
            {
                CancelPieceSelection();
            }

        }

        private void CancelPieceSelection()
        {
            if (currentState != InputState.None)
            {
                currentState = InputState.None;
                boardView.DeselectSquare(selectedPieceSquare);
                boardView.ResetPiecePosition(selectedPieceSquare);
            }
        }

        private void TryMakeMove(Coord startSquare, Coord targetSquare)
        {
            int startIndex = BoardRepresentation.IndexFromCoord(startSquare);
            int targetIndex = BoardRepresentation.IndexFromCoord(targetSquare);
            bool moveIsLegal = false;
            Move chosenMove = new Move();

            MoveGenerator moveGenerator = new MoveGenerator();
            bool wantsKnightPromotion = Input.GetKey(KeyCode.LeftAlt);

            var legalMoves = moveGenerator.GenerateMoves(board);
            for (int i = 0; i < legalMoves.Count; i++)
            {
                var legalMove = legalMoves[i];

                if (legalMove.StartSquare == startIndex && legalMove.TargetSquare == targetIndex)
                {
                    if (legalMove.IsPromotion)
                    {
                        if (legalMove.MoveFlag == Move.Flag.PromoteToQueen && wantsKnightPromotion)
                        {
                            continue;
                        }
                        if (legalMove.MoveFlag != Move.Flag.PromoteToQueen && !wantsKnightPromotion)
                        {
                            continue;
                        }
                    }
                    moveIsLegal = true;
                    chosenMove = legalMove;
                    break;
                }
            }

            if (moveIsLegal)
            {
                ChoseMove(chosenMove);
                currentState = InputState.None;
            }
            else
            {
                CancelPieceSelection();
            }
        }

        private void HandlePieceSelection(Vector2 mousePos)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (boardView.TryGetSquareUnderMouse(mousePos, out selectedPieceSquare))
                {
                    int index = BoardRepresentation.IndexFromCoord(selectedPieceSquare);

                    if (Piece.IsColor(board.Square[index], board.ColorToMove))
                    {
                        boardView.HighlightLegalMoves(board, selectedPieceSquare);
                        boardView.SelectSquare(selectedPieceSquare);
                        currentState = InputState.DraggingPiece;
                    }
                }
            }
        }
    }
}