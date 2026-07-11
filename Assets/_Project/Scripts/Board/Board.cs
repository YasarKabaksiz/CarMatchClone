using System;
using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Core.Events;
using CarMatchClone.Core.Pooling;
using CarMatchClone.Data;
using CarMatchClone.Gameplay;
using CarMatchClone.SpecialMechanics;

namespace CarMatchClone.Board
{
    public class Board : MonoBehaviour
    {
        [Serializable]
        private struct FruitPrefabEntry
        {
            public FruitType fruitType;
            public GameObject prefab;
        }

        [SerializeField] private LevelData _levelData;
        [SerializeField] private FruitPrefabEntry[] _fruitPrefabs;
        [SerializeField] private ObjectPoolManager _poolManager;
        [SerializeField] private FruitEventChannel _onFruitSelectedChannel;
        [SerializeField] private CellEventChannel _onCellVacatedChannel;
        [SerializeField] private VoidEventChannel _onBoardStateChangedChannel;

        [SerializeField] private FruitEventChannel _onBeforeFruitRemovedChannel;

        [SerializeField] private GameObject _wallPrefab;
        [SerializeField] private GameObject _lockedBoxPrefab;
        [SerializeField] private GameObject _garageSpawnerPrefab;
        [SerializeField] private CarMatchClone.Core.Events.ObstacleEventChannel _onObstacleTriggeredChannel;

        private Dictionary<Vector2Int, GridCell> _cells;
        private Dictionary<FruitType, GameObject> _prefabByFruitType;
        private List<GameObject> _wallObjects = new List<GameObject>();
        private List<ILaneObstacle> _obstacles = new List<ILaneObstacle>();
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
            // _levelData null olabilir — GameManager.Start() RebuildGrid ile yükleyecek.
            if (_levelData == null) { Debug.LogWarning("[Board] LevelData atanmamış — GameManager yükleyecek."); return; }
            if (_poolManager == null) { Debug.LogError("[Board] ObjectPoolManager atanmamış."); return; }
            BuildPrefabLookup();
            WarmUpPool();
            BuildGrid();
        }

        private void OnEnable()
        {
            if (_onFruitSelectedChannel == null)
            {
                Debug.LogWarning("[Board] OnFruitSelectedChannel atanmamış — meyve seçimi çalışmaz.");
                return;
            }
            _onFruitSelectedChannel.Subscribe(HandleFruitSelected);
        }

        private void OnDisable()
        {
            _onFruitSelectedChannel?.Unsubscribe(HandleFruitSelected);
        }

        private void HandleFruitSelected(Fruit fruit)
        {
            // UndoBooster'ın snapshot'ı alması için hücre boşaltılmadan önce event fırlatılır.
            _onBeforeFruitRemovedChannel?.Raise(fruit);

            var cell = GetCell(fruit.GridPosition);
            if (cell == null) return;

            cell.Occupant = null;
            cell.IsWalkable = true;

            _onCellVacatedChannel.Raise(cell);
        }

        public bool IsBoardEmpty()
        {
            foreach (var cell in _cells.Values)
                if (cell.Occupant != null) return false;
            return true;
        }

        public void RebuildGrid(LevelData levelData)
        {
            ReleaseAllFruits();
            DestroyWalls();
            DestroyObstacles();
            _levelData = levelData;
            BuildPrefabLookup();
            WarmUpPool();
            BuildGrid();
            // PathfindingService'i yeni grid için tetikle (Start()'taki ilk hesaplama level geçişinde yetmez).
            _onBoardStateChangedChannel?.Raise();
        }

        [ContextMenu("Rebuild Grid (Test)")]
        private void RebuildGridTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Board] Rebuild Grid (Test) sadece Play modunda çalışır.");
                return;
            }
            RebuildGrid(_levelData);
        }

        private void BuildPrefabLookup()
        {
            _prefabByFruitType = new Dictionary<FruitType, GameObject>();
            foreach (var entry in _fruitPrefabs)
                _prefabByFruitType[entry.fruitType] = entry.prefab;
        }

        private void WarmUpPool()
        {
            var countByType = new Dictionary<FruitType, int>();
            foreach (var entry in _levelData.cells)
            {
                if (entry.type == CellType.CarSlot)
                {
                    if (!countByType.ContainsKey(entry.color)) countByType[entry.color] = 0;
                    countByType[entry.color]++;
                }
                else if (entry.type == CellType.GarageSpawner && entry.garageColors != null)
                {
                    foreach (var c in entry.garageColors)
                    {
                        if (!countByType.ContainsKey(c)) countByType[c] = 0;
                        countByType[c]++;
                    }
                }
            }

            foreach (var entry in _fruitPrefabs)
            {
                if (countByType.TryGetValue(entry.fruitType, out int count))
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

                switch (entry.type)
                {
                    case CellType.Wall:
                        SpawnWall(entry.position);
                        break;
                    case CellType.CarSlot:
                        SpawnFruitAtCell(cell, entry.color);
                        break;
                    case CellType.LockedBox:
                        SpawnLockedBox(entry);
                        break;
                    case CellType.GarageSpawner:
                        SpawnGarageSpawner(entry);
                        break;
                }
            }
        }

        private void SpawnWall(Vector2Int pos)
        {
            if (_wallPrefab == null) return;
            var wall = Instantiate(_wallPrefab, GridToWorld(pos), Quaternion.identity, transform);
            wall.name = $"Wall_{pos.x}_{pos.y}";
            _wallObjects.Add(wall);
        }

        private void SpawnLockedBox(LevelData.CellEntry entry)
        {
            if (_lockedBoxPrefab == null)
            {
                Debug.LogWarning($"[Board] LockedBox prefab atanmamış — {entry.position} hücresi atlandı.");
                return;
            }
            var obj = Instantiate(_lockedBoxPrefab, GridToWorld(entry.position), Quaternion.identity, transform);
            obj.name = $"LockedBox_{entry.position.x}_{entry.position.y}";
            var lb = obj.GetComponent<LockedBox>();
            if (lb == null) { Debug.LogError("[Board] LockedBox prefab üzerinde LockedBox component yok."); return; }
            lb.Initialize(entry.position, this, _onCellVacatedChannel);
            lb.SetObstacleChannel(_onObstacleTriggeredChannel);
            _obstacles.Add(lb);
        }

        private void SpawnGarageSpawner(LevelData.CellEntry entry)
        {
            if (_garageSpawnerPrefab == null)
            {
                Debug.LogWarning($"[Board] GarageSpawner prefab atanmamış — {entry.position} hücresi atlandı.");
                return;
            }
            var obj = Instantiate(_garageSpawnerPrefab, GridToWorld(entry.position), Quaternion.identity, transform);
            obj.name = $"GarageSpawner_{entry.position.x}_{entry.position.y}";
            var gs = obj.GetComponent<GarageSpawner>();
            if (gs == null) { Debug.LogError("[Board] GarageSpawner prefab üzerinde GarageSpawner component yok."); return; }
            gs.Initialize(entry.position, this, _onCellVacatedChannel);
            gs.Setup(entry.garageColors ?? System.Array.Empty<FruitType>(), entry.facingDirection, _onObstacleTriggeredChannel);
            _obstacles.Add(gs);
        }

        private void SpawnFruitAtCell(GridCell cell, FruitType fruitType)
        {
            if (!_prefabByFruitType.TryGetValue(fruitType, out var prefab))
            {
                Debug.LogError($"[Board] {fruitType} tipi için prefab atanmamış.");
                return;
            }

            GameObject fruitObj = _poolManager.Get(prefab);
            fruitObj.transform.SetPositionAndRotation(GridToWorld(cell.Position), Quaternion.identity);
            fruitObj.transform.SetParent(transform);
            fruitObj.SetActive(true);
            fruitObj.name = $"Fruit_{cell.Position.x}_{cell.Position.y}";

            var fruit = fruitObj.GetComponent<Fruit>();
            if (fruit == null)
                fruit = fruitObj.AddComponent<Fruit>();

            fruit.GridPosition = cell.Position;
            fruit.Color = fruitType;
            fruit.SourcePrefab = prefab;
            cell.Occupant = fruit;
        }

        // LockedBox tarafından tetiklenir; LevelData'dan gizli meyve tipini okur.
        public void RevealLockedBox(Vector2Int pos)
        {
            var cell = GetCell(pos);
            if (cell == null) return;

            FruitType hiddenFruitType = default;
            foreach (var entry in _levelData.cells)
            {
                if (entry.position == pos && entry.type == CellType.LockedBox)
                {
                    hiddenFruitType = entry.color;
                    break;
                }
            }

            SpawnFruitAtCell(cell, hiddenFruitType);
            _onBoardStateChangedChannel?.Raise();
        }

        // GarageSpawner tarafından tetiklenir; facingCell'e meyve spawn eder.
        public void SpawnFromGarage(Vector2Int pos, FruitType fruitType)
        {
            var cell = GetCell(pos);
            if (cell == null || cell.Occupant != null) return;

            SpawnFruitAtCell(cell, fruitType);
            _onBoardStateChangedChannel?.Raise();
        }

        // UndoBooster: GarageSpawner undo — spawned meyveyi siler, hücreyi walkable yapar.
        // OnBoardStateChanged FIRALATMAZ — akış sonunda PlaceFruitBack zaten tetikler.
        public bool RemoveFruitAt(Vector2Int pos)
        {
            var cell = GetCell(pos);
            if (cell == null || cell.Occupant == null) return false;
            _poolManager.Release(cell.Occupant.SourcePrefab, cell.Occupant.gameObject);
            cell.Occupant = null;
            cell.IsWalkable = true;
            return true;
        }

        // UndoBooster: LockedBox undo — revealed meyveyi siler, hücre BLOKE kalır.
        // LockedBox hücresi kutu aktifken hiçbir zaman walkable olmamalı.
        public bool RemoveFruitAtAndBlock(Vector2Int pos)
        {
            var cell = GetCell(pos);
            if (cell == null || cell.Occupant == null) return false;
            _poolManager.Release(cell.Occupant.SourcePrefab, cell.Occupant.gameObject);
            cell.Occupant = null;
            cell.IsWalkable = false;
            return true;
        }

        // UndoBooster: son hamlede boşalan hücreye meyveyi geri koyar.
        public bool PlaceFruitBack(Vector2Int pos, FruitType fruitType)
        {
            var cell = GetCell(pos);
            if (cell == null || cell.Occupant != null) return false;

            cell.IsWalkable = false;
            SpawnFruitAtCell(cell, fruitType);
            _onBoardStateChangedChannel?.Raise();
            return true;
        }

        // ShuffleBooster: board'daki mevcut meyvelerin tiplerini Fisher-Yates ile karıştırır.
        public void ShuffleFruitTypes()
        {
            var occupiedCells = new List<GridCell>();
            var fruitTypes = new List<FruitType>();

            foreach (var cell in _cells.Values)
            {
                if (cell.Occupant != null)
                {
                    occupiedCells.Add(cell);
                    fruitTypes.Add(cell.Occupant.Color);
                }
            }

            if (occupiedCells.Count <= 1) return;

            foreach (var cell in occupiedCells)
            {
                _poolManager.Release(cell.Occupant.SourcePrefab, cell.Occupant.gameObject);
                cell.Occupant = null;
            }

            var rng = new System.Random();
            for (int i = fruitTypes.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = fruitTypes[i]; fruitTypes[i] = fruitTypes[j]; fruitTypes[j] = tmp;
            }

            for (int i = 0; i < occupiedCells.Count; i++)
                SpawnFruitAtCell(occupiedCells[i], fruitTypes[i]);

            _onBoardStateChangedChannel?.Raise();
        }

        private void ReleaseAllFruits()
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

        private void DestroyObstacles()
        {
            foreach (var obstacle in _obstacles)
            {
                if (obstacle is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            }
            _obstacles.Clear();
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
