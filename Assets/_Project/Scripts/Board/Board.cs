using System;
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
        [Serializable]
        private struct CarPrefabEntry
        {
            public CarColor color;
            public GameObject prefab;
        }

        [SerializeField] private LevelData _levelData;
        [SerializeField] private CarPrefabEntry[] _carPrefabs;
        [SerializeField] private ObjectPoolManager _poolManager;
        [SerializeField] private CarEventChannel _onCarSelectedChannel;
        [SerializeField] private CellEventChannel _onCellVacatedChannel;
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;

        [SerializeField] private GameObject _wallPrefab;

        private Dictionary<Vector2Int, GridCell> _cells;
        private Dictionary<CarColor, GameObject> _prefabByColor;
        private List<GameObject> _wallObjects = new List<GameObject>();
        private Vector3 _centerOffset;

        private const int BoardWidth = 7;
        private static readonly Vector2Int[] _exitPositions = BuildExitPositions();
        public Vector2Int[] ExitPositions => _exitPositions;

        private static Vector2Int[] BuildExitPositions()
        {
            var exits = new Vector2Int[BoardWidth];
            for (int x = 0; x < BoardWidth; x++)
                exits[x] = new Vector2Int(x, -1);
            return exits;
        }

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
            if (_levelData.cells.Length != 56)
                Debug.LogWarning($"[Board] LevelData 56 hücre içermeli, {_levelData.cells.Length} var — {_levelData.name}");
            BuildPrefabLookup();
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
            DestroyWalls();
            _levelData = levelData;
            BuildPrefabLookup();
            WarmUpPool();
            BuildGrid();
        }

        // Inspector sağ tık → "Rebuild Grid (Test)": mevcut LevelData ile grid'i yeniler.
        [ContextMenu("Rebuild Grid (Test)")]
        private void RebuildGridTest() => RebuildGrid(_levelData);

        private void BuildPrefabLookup()
        {
            _prefabByColor = new Dictionary<CarColor, GameObject>();
            foreach (var entry in _carPrefabs)
                _prefabByColor[entry.color] = entry.prefab;
        }

        private void WarmUpPool()
        {
            var countByColor = new Dictionary<CarColor, int>();
            foreach (var entry in _levelData.cells)
            {
                if (entry.type != CellType.CarSlot) continue;
                if (!countByColor.ContainsKey(entry.color))
                    countByColor[entry.color] = 0;
                countByColor[entry.color]++;
            }

            foreach (var entry in _carPrefabs)
            {
                if (countByColor.TryGetValue(entry.color, out int count))
                    _poolManager.WarmUp(entry.prefab, count);
            }
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

                if (entry.type == CellType.Wall && _wallPrefab != null)
                {
                    var wall = Instantiate(_wallPrefab, GridToWorld(entry.position), Quaternion.identity, transform);
                    wall.name = $"Wall_{entry.position.x}_{entry.position.y}";
                    _wallObjects.Add(wall);
                }

                if (entry.type == CellType.CarSlot)
                {
                    if (!_prefabByColor.TryGetValue(entry.color, out var prefab))
                    {
                        Debug.LogError($"[Board] {entry.color} rengi için prefab atanmamış.");
                        continue;
                    }

                    Vector3 worldPos = GridToWorld(entry.position);
                    GameObject carObj = _poolManager.Get(prefab);
                    carObj.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
                    carObj.transform.SetParent(transform);
                    carObj.SetActive(true);
                    carObj.name = $"Car_{entry.position.x}_{entry.position.y}";

                    var car = carObj.GetComponent<Car>();
                    if (car == null)
                        car = carObj.AddComponent<Car>();

                    car.GridPosition = entry.position;
                    car.Color = entry.color;
                    car.SourcePrefab = prefab;
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
                    _poolManager.Release(cell.Occupant.SourcePrefab, cell.Occupant.gameObject);
            }
            _cells.Clear();
        }

        private void DestroyWalls()
        {
            foreach (var wall in _wallObjects)
                if (wall != null) Destroy(wall);
            _wallObjects.Clear();
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

        public Vector3 GridToWorld(Vector2Int pos)
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
