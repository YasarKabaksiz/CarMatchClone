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
            public CarColor color;
        }

        public CellEntry[] cells;
        public float cellSize = 1.5f;

        private void OnValidate()
        {
            if (cells != null && cells.Length != 0 && cells.Length != 56)
                Debug.LogWarning($"[LevelData] 56 hücre bekleniyor, {cells.Length} var — {name}");
        }
    }
}
