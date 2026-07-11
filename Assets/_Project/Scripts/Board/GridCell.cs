using UnityEngine;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Board
{
    [System.Serializable]
    public class GridCell
    {
        public Vector2Int Position { get; }
        public bool IsWalkable { get; set; }
        public Fruit Occupant { get; set; }

        public GridCell(Vector2Int position, bool isWalkable = true)
        {
            Position = position;
            IsWalkable = isWalkable;
        }
    }
}
