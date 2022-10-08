using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Chess
{
    public class GameController : MonoBehaviour
    {
        public enum Result { Playing, WhiteIsMated, BlackIsMated, Stalemate, Repetition, FiftyMoveRule, InsufficientMaterial }

        public event System.Action onPositionLoaded;
        public event System.Action<Move> onMoveMade;

        public bool loadCustomPosition;
        public string customPosition = "1rbq1r1k/2pp2pp/p1n3p1/2b1p3/R3P3/1BP2N2/1P3PPP/1NBQ1RK1 w - - 0 1";

        public TextMeshProUGUI resultUI;

        private Result gameResult;

        private Player whitePlayer;
        private Player blackPlayer;
        private Player playerToMove;
        private List<Move> gameMoves;
        private BoardView boardView;

        private Board searchBoard;

        public Board board;

        private void Start()
        {
            boardView = FindObjectOfType<BoardView>();
            gameMoves = new List<Move>();
            board = new Board();
            searchBoard = new Board();

            NewGame(Random.Range(0, 2) > 0);
        }

        private void Update()
        {
            if (gameResult == Result.Playing)
            {
                playerToMove.Update();
            }

            if (Input.GetKey(KeyCode.Q))
            {
                QuitGame();
            }
        }

        private void OnMoveChosen(Move move)
        {
            board.MakeMove(move);
            searchBoard.MakeMove(move);

            gameMoves.Add(move);
            onMoveMade?.Invoke(move);
            boardView.OnMoveMade(board, move, true);

            ProcessGameState();
        }

        public void NewGame(bool playsWhite)
        {
            NewGame();
            boardView.SetPerspective(playsWhite);
        }

        private void NewGame()
        {
            gameMoves.Clear();
            if (loadCustomPosition)
            {
                board.LoadPosition(customPosition);
                searchBoard.LoadPosition(customPosition);
            }
            else
            {
                board.LoadStartPosition();
                searchBoard.LoadStartPosition();
            }

            onPositionLoaded?.Invoke();
            boardView.UpdatePosition(board);
            boardView.ResetSquareColors();

            CreatePlayer(ref whitePlayer);
            CreatePlayer(ref blackPlayer);

            ProcessGameState();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private void ProcessGameState()
        {
            gameResult = GetGameState();
            PrintGameResult(gameResult);

            if (gameResult != Result.Playing) return;
            
            playerToMove = board.WhiteToMove ? whitePlayer : blackPlayer;
        }

        private void PrintGameResult(Result result)
        {
            switch (result)
            {
                case Result.Playing:
                    resultUI.text = "";
                    break;
                case Result.WhiteIsMated:
                case Result.BlackIsMated:
                    resultUI.text = "Checkmate";
                    break;
                case Result.Stalemate:
                case Result.Repetition:
                case Result.FiftyMoveRule:
                case Result.InsufficientMaterial:
                    resultUI.text = "Draw";
                    break;
            }
        }

        private Result GetGameState()
        {
            MoveGenerator moveGenerator = new MoveGenerator();
            var moves = moveGenerator.GenerateMoves(board);

            if (moves.Count == 0)
            {
                if (moveGenerator.InCheck)
                {
                    return (board.WhiteToMove) ? Result.WhiteIsMated : Result.BlackIsMated;
                }
                return Result.Stalemate;
            }

            if (board.fiftyMoveCounter >= 100)
            {
                return Result.FiftyMoveRule;
            }

            int numPawns = board.pawns[Board.WhiteIndex].Count + board.pawns[Board.BlackIndex].Count;
            int numRooks = board.rooks[Board.WhiteIndex].Count + board.rooks[Board.BlackIndex].Count;
            int numQueens = board.queens[Board.WhiteIndex].Count + board.queens[Board.BlackIndex].Count;
            int numKnights = board.knights[Board.WhiteIndex].Count + board.knights[Board.BlackIndex].Count;
            int numBishops = board.bishops[Board.WhiteIndex].Count + board.bishops[Board.BlackIndex].Count;

            if (numPawns + numRooks + numQueens == 0)
            {
                if (numKnights == 1 || numBishops == 1)
                {
                    return Result.InsufficientMaterial;
                }
            }

            return Result.Playing;
        }

        private void CreatePlayer(ref Player player)
        {
            player = new Player(board);

            player.OnMoveChosen += OnMoveChosen;
        }
    }
}