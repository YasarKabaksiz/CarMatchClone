using UnityEngine;
using CarMatchClone.Board;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;

namespace CarMatchClone.SpecialMechanics
{
    public class GarageSpawner : MonoBehaviour, ILaneObstacle
    {
        private Vector2Int _gridPos;
        private Vector2Int _facingCell;
        private CarMatchClone.Board.Board _board;
        private CellEventChannel _onCellVacatedChannel;
        private CarColor _spawnColor;
        private int _stockCount;

        public bool IsActive => _stockCount > 0;

        public void Initialize(Vector2Int gridPos, CarMatchClone.Board.Board board, CellEventChannel onCellVacatedChannel)
        {
            _gridPos = gridPos;
            _board = board;
            _onCellVacatedChannel = onCellVacatedChannel;
            _onCellVacatedChannel.Subscribe(OnCellVacated);
        }

        // Initialize'dan SONRA çağrılmalı (_gridPos kullanır).
        public void Setup(CarColor color, FacingDirection facing, int stockCount)
        {
            _spawnColor = color;
            _facingCell = _gridPos + facing.ToVector();
            _stockCount = stockCount;
        }

        private void OnDisable()
        {
            _onCellVacatedChannel?.Unsubscribe(OnCellVacated);
        }

        private void OnCellVacated(GridCell cell)
        {
            if (_stockCount <= 0) return;

            // Tek tetikleyici: garajın "önündeki" hücre boşaldı.
            // _gridPos asla araç almaz, dolayısıyla vacated event'i hiç gelmez.
            if (cell.Position != _facingCell) return;

            Trigger();
        }

        private void Trigger()
        {
            _stockCount--;
            // Araç, garajın kendi hücresine değil facingCell'e spawn olur.
            _board.SpawnFromGarage(_facingCell, _spawnColor);
        }
    }
}
