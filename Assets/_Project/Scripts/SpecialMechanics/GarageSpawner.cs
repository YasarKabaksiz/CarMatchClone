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
        private FruitType[] _fruitTypes;
        private int _currentSpawnIndex;

        public bool IsActive => _fruitTypes != null && _currentSpawnIndex < _fruitTypes.Length;

        public void Initialize(Vector2Int gridPos, CarMatchClone.Board.Board board, CellEventChannel onCellVacatedChannel)
        {
            _gridPos = gridPos;
            _board = board;
            _onCellVacatedChannel = onCellVacatedChannel;
            _onCellVacatedChannel.Subscribe(OnCellVacated);
        }

        // Board.SpawnGarageSpawner tarafından Initialize'dan sonra çağrılır.
        public void Setup(FruitType[] fruitTypes, FacingDirection facing, ObstacleEventChannel onObstacleTriggeredChannel)
        {
            _fruitTypes = fruitTypes;
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
            var fruitType = _fruitTypes[_currentSpawnIndex];
            _currentSpawnIndex++;
            _board.SpawnFromGarage(_facingCell, fruitType);
            _onObstacleTriggeredChannel?.Raise(new ObstacleTriggerPayload
            {
                Position = _facingCell,
                UndoAction = UndoLastSpawn
            });
        }

        private void UndoLastSpawn()
        {
            _currentSpawnIndex--;
            _board.RemoveFruitAt(_facingCell);
        }
    }
}
