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
            public Vector2Int    position;
            public CellType      type;
            public CarColor      color;           // CarSlot: araç rengi | LockedBox: gizli araç rengi
            public FacingDirection facingDirection; // Yalnızca GarageSpawner için
            public CarColor[]    garageColors;    // Yalnızca GarageSpawner: sıralı spawn renkleri; uzunluk = stok sayısı
        }

        public CellEntry[] cells;
        public float cellSize = 1.5f;
    }
}
