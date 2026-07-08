using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Core.Events;
using CarMatchClone.Core.Pooling;
using CarMatchClone.Data;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Board
{
    public class Board : MonoBehaviour
    {
        [SerializeField] private LevelData _levelData;
        [SerializeField] private GameObject _carPrefab;
        [SerializeField] private ObjectPoolManager _poolManager;
        [SerializeField] private CarEventChannel _onCarSelectedChannel;
        [SerializeField] private CellEventChannel _onCellVacatedChannel;
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;

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
            if (_poolManager == null)
            {
                Debug.LogError("[Board] ObjectPoolManager atanmamış.");
                return;
            }
            WarmUpPool();
            BuildGrid();
        }

        private void OnEnable()
        {
            if (_onCarSelectedChannel == null)
            {
                Debug.LogWarning("[Board] OnCarSelectedChannel atanmamış — araç seçimi çalışmaz.");
                return;
            }
            _onCarSelectedChannel.Subscribe(HandleCarSelected);
        }

        private void OnDisable()
        {
            _onCarSelectedChannel?.Unsubscribe(HandleCarSelected);
        }

        private void HandleCarSelected(Car car)
        {
            var cell = GetCell(car.GridPosition);
            if (cell == null) return;

            cell.Occupant = null;
            cell.IsWalkable = true;

            _onCellVacatedChannel.Raise(cell);

            if (IsBoardEmpty())
                _onLevelCompleteChannel.Raise();
        }

        private bool IsBoardEmpty()
        {
            foreach (var cell in _cells.Values)
                if (cell.Occupant != null) return false;
            return true;
        }

        // LevelLoader'ın Milestone 5'te çağıracağı kalıcı public API.
        public void RebuildGrid(LevelData levelData)
        {
            ReleaseAllCars();
            _levelData = levelData;
            BuildGrid();
        }

        // Inspector sağ tık → "Rebuild Grid (Test)": mevcut LevelData ile grid'i yeniler.
        [ContextMenu("Rebuild Grid (Test)")]
        private void RebuildGridTest() => RebuildGrid(_levelData);

        private void WarmUpPool()
        {
            int carCount = 0;
            foreach (var entry in _levelData.cells)
                if (entry.type == CellType.CarSlot) carCount++;

            _poolManager.WarmUp(_carPrefab, carCount);
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

                    GameObject carObj = _poolManager.Get(_carPrefab);
                    carObj.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
                    carObj.transform.SetParent(transform);
                    carObj.SetActive(true);
                    carObj.name = $"Car_{entry.position.x}_{entry.position.y}";

                    var car = carObj.GetComponent<Car>();
                    if (car == null)
                        car = carObj.AddComponent<Car>();

                    car.GridPosition = entry.position;
                    car.Color = entry.color;
                    cell.Occupant = car;
                }
            }
        }

        private void ReleaseAllCars()
        {
            if (_cells == null) return;
            foreach (var cell in _cells.Values)
            {
                if (cell.Occupant != null)
                    _poolManager.Release(_carPrefab, cell.Occupant.gameObject);
            }
            _cells.Clear();
        }

        public GridCell GetCell(int x, int y) => GetCell(new Vector2Int(x, y));

        public GridCell GetCell(Vector2Int pos) =>
            _cells.TryGetValue(pos, out var cell) ? cell : null;

        public IEnumerable<GridCell> GetAllCells() => _cells.Values;

        public Bounds GetWorldBounds()
        {
            if (_cells == null || _cells.Count == 0)
                return new Bounds(transform.position, Vector3.zero);

            bool first = true;
            Bounds bounds = default;
            foreach (var pos in _cells.Keys)
            {
                Vector3 worldPos = GridToWorld(pos);
                if (first) { bounds = new Bounds(worldPos, Vector3.zero); first = false; }
                else bounds.Encapsulate(worldPos);
            }
            return bounds;
        }

        private Vector3 GridToWorld(Vector2Int pos)
        {
            float s = _levelData.cellSize;
            return transform.position + new Vector3(
                pos.x * s - _centerOffset.x,
                0f,
                pos.y * s - _centerOffset.z);
        }

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
