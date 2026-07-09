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
        private CarColor[] _garageColors;
        private int _currentSpawnIndex;

        public bool IsActive => _garageColors != null && _currentSpawnIndex < _garageColors.Length;

        public void Initialize(Vector2Int gridPos, CarMatchClone.Board.Board board, CellEventChannel onCellVacatedChannel)
        {
            _gridPos = gridPos;
            _board = board;
            _onCellVacatedChannel = onCellVacatedChannel;
            _onCellVacatedChannel.Subscribe(OnCellVacated);
        }

        // Board.SpawnGarageSpawner tarafından Initialize'dan sonra çağrılır.
        public void Setup(CarColor[] garageColors, FacingDirection facing, ObstacleEventChannel onObstacleTriggeredChannel)
        {
            _garageColors = garageColors;
            _currentSpawnIndex = 0;
            _facingCell = _gridPos + facing.ToVector();
            _onObstacleTriggeredChannel = onObstacleTriggeredChannel;
        }

        private void OnDisable()
        {
            _onCellVacatedChannel?.Unsubscribe(OnCellVacated);
        }

        private void OnCellVacated(GridCell cell)
        {
            if (!IsActive) return;
            if (cell.Position != _facingCell) return;
            Trigger();
        }

        private void Trigger()
        {
            var spawnColor = _garageColors[_currentSpawnIndex];
            _currentSpawnIndex++;
            _board.SpawnFromGarage(_facingCell, spawnColor);
            _onObstacleTriggeredChannel?.Raise(new ObstacleTriggerPayload
            {
                Position = _facingCell,
                UndoAction = UndoLastSpawn
            });
        }

        private void UndoLastSpawn()
        {
            _currentSpawnIndex--;
            _board.RemoveCarAt(_facingCell);
        }
    }
}
