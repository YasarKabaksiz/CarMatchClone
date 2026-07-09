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
        private ObstacleEventChannel _onObstacleTriggeredChannel;
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

        // Board.SpawnGarageSpawner tarafından Initialize'dan sonra çağrılır.
        public void Setup(CarColor color, FacingDirection facing, int stockCount, ObstacleEventChannel onObstacleTriggeredChannel)
        {
            _spawnColor = color;
            _facingCell = _gridPos + facing.ToVector();
            _stockCount = stockCount;
            _onObstacleTriggeredChannel = onObstacleTriggeredChannel;
        }

        private void OnDisable()
        {
            _onCellVacatedChannel?.Unsubscribe(OnCellVacated);
        }

        private void OnCellVacated(GridCell cell)
        {
            if (_stockCount <= 0) return;
            if (cell.Position != _facingCell) return;
            Trigger();
        }

        private void Trigger()
        {
            _stockCount--;
            _board.SpawnFromGarage(_facingCell, _spawnColor);
            _onObstacleTriggeredChannel?.Raise(new ObstacleTriggerPayload
            {
                Position = _facingCell,
                UndoAction = UndoLastSpawn
            });
        }

        private void UndoLastSpawn()
        {
            _stockCount++;
            _board.RemoveCarAt(_facingCell);
        }
    }
}
