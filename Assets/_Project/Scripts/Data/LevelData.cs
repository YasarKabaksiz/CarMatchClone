using System;
using UnityEngine;

namespace CarMatchClone.Data
{
    [CreateAssetMenu(menuName = "CarMatchClone/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Serializable]
        public class CellEntry
        {
            public Vector2Int position;
            public CellType type;
        }

        public CellEntry[] cells;
        public Vector2Int[] exitPositions;
        public float cellSize = 1.5f;
    }
}
