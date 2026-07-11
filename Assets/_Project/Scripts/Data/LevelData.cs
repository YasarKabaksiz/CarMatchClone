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
            public FruitType     color;           // CarSlot: meyve tipi | LockedBox: gizli meyve tipi
            public FacingDirection facingDirection; // Yalnızca GarageSpawner için
            public FruitType[]   garageColors;    // Yalnızca GarageSpawner: sıralı spawn tipleri; uzunluk = stok sayısı
        }

        public CellEntry[] cells;
        public float cellSize = 1.5f;
    }
}
