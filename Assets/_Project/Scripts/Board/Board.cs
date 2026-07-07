using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Board
{
    public class Board : MonoBehaviour
    {
        [SerializeField] private int _gridWidth = 5;
        [SerializeField] private int _gridHeight = 1;
        [SerializeField] private float _cellSize = 1.5f;
        [SerializeField] private GameObject _carPrefab;

        private GridCell[,] _cells;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        // Virtual exit point: one step right of the last column, row 0.
        // Cars must reach this to enter the Holder.
        public Vector2Int ExitPosition => new Vector2Int(_gridWidth, 0);

        private void Awake()
        {
            BuildGrid();
        }

        private void BuildGrid()
        {
            _cells = new GridCell[_gridWidth, _gridHeight];

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = new GridCell(pos, isWalkable: false); // blocked by occupying car
                    _cells[x, y] = cell;

                    Vector3 worldPos = GridToWorld(x, y);
                    GameObject carObj = Instantiate(_carPrefab, worldPos, Quaternion.identity, transform);
                    carObj.name = $"Car_{x}_{y}";

                    var car = carObj.GetComponent<Car>();
                    if (car == null)
                        car = carObj.AddComponent<Car>();

                    car.GridPosition = pos;
                    cell.Occupant = car;
                }
            }
        }

        public GridCell GetCell(int x, int y)
        {
            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                return null;
            return _cells[x, y];
        }

        public GridCell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public IEnumerable<GridCell> GetAllCells()
        {
            for (int x = 0; x < _gridWidth; x++)
                for (int y = 0; y < _gridHeight; y++)
                    yield return _cells[x, y];
        }

        private Vector3 GridToWorld(int x, int y)
        {
            float offsetX = (_gridWidth - 1) * _cellSize * 0.5f;
            float offsetZ = (_gridHeight - 1) * _cellSize * 0.5f;
            return transform.position + new Vector3(x * _cellSize - offsetX, 0f, y * _cellSize - offsetZ);
        }
    }
}
