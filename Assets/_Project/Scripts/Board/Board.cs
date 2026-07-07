using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Data;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Board
{
    public class Board : MonoBehaviour
    {
        [SerializeField] private LevelData _levelData;
        [SerializeField] private GameObject _carPrefab;

        private Dictionary<Vector2Int, GridCell> _cells;
        private Vector3 _centerOffset;

        public Vector2Int[] ExitPositions => _levelData.exitPositions;

        private void Awake()
        {
            if (_levelData == null)
            {
                Debug.LogError("[Board] LevelData atanmamış.");
                return;
            }
            BuildGrid();
        }

        private void BuildGrid()
        {
            _cells = new Dictionary<Vector2Int, GridCell>();
            _centerOffset = ComputeCenterOffset();

            foreach (var entry in _levelData.cells)
            {
                bool walkable = entry.type == CellType.Empty;
                var cell = new GridCell(entry.position, isWalkable: walkable);
                _cells[entry.position] = cell;

                if (entry.type == CellType.CarSlot)
                {
                    Vector3 worldPos = GridToWorld(entry.position);
                    GameObject carObj = Instantiate(_carPrefab, worldPos, Quaternion.identity, transform);
                    carObj.name = $"Car_{entry.position.x}_{entry.position.y}";

                    var car = carObj.GetComponent<Car>();
                    if (car == null)
                        car = carObj.AddComponent<Car>();

                    car.GridPosition = entry.position;
                    cell.Occupant = car;
                }
            }
        }

        public GridCell GetCell(int x, int y) => GetCell(new Vector2Int(x, y));

        public GridCell GetCell(Vector2Int pos) =>
            _cells.TryGetValue(pos, out var cell) ? cell : null;

        public IEnumerable<GridCell> GetAllCells() => _cells.Values;

        private Vector3 GridToWorld(Vector2Int pos)
        {
            float s = _levelData.cellSize;
            return transform.position + new Vector3(
                pos.x * s - _centerOffset.x,
                0f,
                pos.y * s - _centerOffset.z);
        }

        // Bounding-box ortası: arbitrary şekiller için genel centering.
        private Vector3 ComputeCenterOffset()
        {
            if (_levelData.cells == null || _levelData.cells.Length == 0)
                return Vector3.zero;

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var entry in _levelData.cells)
            {
                if (entry.position.x < minX) minX = entry.position.x;
                if (entry.position.x > maxX) maxX = entry.position.x;
                if (entry.position.y < minY) minY = entry.position.y;
                if (entry.position.y > maxY) maxY = entry.position.y;
            }

            float s = _levelData.cellSize;
            return new Vector3(
                (minX + maxX) * 0.5f * s,
                0f,
                (minY + maxY) * 0.5f * s);
        }
    }
}
