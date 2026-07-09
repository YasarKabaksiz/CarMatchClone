using UnityEngine;
using CarMatchClone.Board;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;

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

        [SerializeField] private bool _debugLogging;

        private Vector2Int _gridPos;
        private CarMatchClone.Board.Board _board;
        private CellEventChannel _onCellVacatedChannel;
        private ObstacleEventChannel _onObstacleTriggeredChannel;
        private bool _triggered;

        private static int _seq;

        public bool IsActive => !_triggered;

        public void Initialize(Vector2Int gridPos, CarMatchClone.Board.Board board, CellEventChannel onCellVacatedChannel)
        {
            _gridPos = gridPos;
            _board = board;
            _onCellVacatedChannel = onCellVacatedChannel;
            _onCellVacatedChannel.Subscribe(OnCellVacated);
        }

        // Board.SpawnLockedBox tarafından Initialize'dan sonra çağrılır.
        public void SetObstacleChannel(ObstacleEventChannel onObstacleTriggeredChannel)
        {
            _onObstacleTriggeredChannel = onObstacleTriggeredChannel;
        }

        // SetActive(true) sonrası aboneliği yeniler (undo akışında gerekli).
        // İlk aktivasyonda _onCellVacatedChannel henüz null → Subscribe çağrılmaz.
        // Initialize() kendi Subscribe'ını yapar; undo sonrası OnEnable devralır.
        private void OnEnable()
        {
            _onCellVacatedChannel?.Subscribe(OnCellVacated);
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
            _onObstacleTriggeredChannel?.Raise(new ObstacleTriggerPayload
            {
                Position = _gridPos,
                UndoAction = UndoLastReveal
            });
        }

        private void UndoLastReveal()
        {
            if (_debugLogging)
            {
                var cell = _board.GetCell(_gridPos);
                Debug.Log($"[#{++_seq}][LockedBox] UndoLastReveal BAŞLADI — _triggered={_triggered}, IsWalkable={cell?.IsWalkable}, Occupant={(cell?.Occupant != null ? cell.Occupant.name : "null")}");
            }

            _triggered = false;

            // Araç silinir; hücre walkable=false kalır (kutu aktifken bloklama devam eder).
            bool removed = _board.RemoveCarAtAndBlock(_gridPos);

            if (_debugLogging)
            {
                var cell = _board.GetCell(_gridPos);
                Debug.Log($"[#{++_seq}][LockedBox] RemoveCarAtAndBlock sonuç={removed} — IsWalkable={cell?.IsWalkable}, Occupant={(cell?.Occupant != null ? cell.Occupant.name : "null")}");
            }

            // SetActive(true) → OnEnable → _onCellVacatedChannel.Subscribe(OnCellVacated)
            gameObject.SetActive(true);

            if (_debugLogging)
                Debug.Log($"[#{++_seq}][LockedBox] SetActive(true) tamamlandı — _triggered={_triggered}, IsActive={IsActive}");
        }
    }
}
