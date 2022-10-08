using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Theme/Board")]
    public class BoardTheme : ScriptableObject
    {
        public Sprite boardTile;
        public SquareColors lightSquares;
        public SquareColors darkSquares;

        [System.Serializable]
        public struct SquareColors
        {
            public Color normal;
            public Color legal;
            public Color selected;
            public Color moveFromHighlight;
            public Color moveToHighlight;
        }
    }
}