using UnityEngine;
using CarMatchClone.Board;
using CarMatchClone.Core.Events;

namespace CarMatchClone.SpecialMechanics
{
    public class LockedBox : MonoBehaviour, ILaneObstacle
    {
        private static readonly Vector2Int[] _neighborOffsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private Vector2Int _gridPos;
        private CarMatchClone.Board.Board _board;
        private CellEventChannel _onCellVacatedChannel;
        private bool _triggered;

        public bool IsActive => !_triggered;

        public void Initialize(Vector2Int gridPos, CarMatchClone.Board.Board board, CellEventChannel onCellVacatedChannel)
        {
            _gridPos = gridPos;
            _board = board;
            _onCellVacatedChannel = onCellVacatedChannel;
            _onCellVacatedChannel.Subscribe(OnCellVacated);
        }

        private void OnDisable()
        {
            _onCellVacatedChannel?.Unsubscribe(OnCellVacated);
        }

        private void OnCellVacated(GridCell cell)
        {
            if (_triggered) return;
            foreach (var offset in _neighborOffsets)
            {
                if (cell.Position == _gridPos + offset)
                {
                    Trigger();
                    return;
                }
            }
        }

        private void Trigger()
        {
            _triggered = true;
            _board.RevealLockedBox(_gridPos);
            gameObject.SetActive(false); // placeholder; M11'de VFX ile değişecek
        }
    }
}
