using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Core;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Boosters
{
    public class UndoBooster : MonoBehaviour, IBooster
    {
        // OnBeforeFruitRemoved: Board'un HandleFruitSelected başında, hücre boşaltılmadan önce fırlatır.
        // Bu sayede RecordSnapshot her zaman GarageSpawner.Trigger()'dan önce çalışır.
        [SerializeField] private FruitEventChannel _onBeforeFruitRemovedChannel;
        [SerializeField] private ObstacleEventChannel _onObstacleTriggeredChannel;
        [SerializeField] private Holder _holder;
        [SerializeField] private bool _debugLogging;

        private bool _hasSnapshot;
        private Vector2Int _snapshotPos;
        private FruitType _snapshotFruitType;
        private readonly List<System.Action> _pendingObstacleUndos = new List<System.Action>();

        private static int _seq;

        private void OnEnable()
        {
            _onBeforeFruitRemovedChannel?.Subscribe(RecordSnapshot);
            _onObstacleTriggeredChannel?.Subscribe(OnObstacleTriggered);
        }

        private void OnDisable()
        {
            _onBeforeFruitRemovedChannel?.Unsubscribe(RecordSnapshot);
            _onObstacleTriggeredChannel?.Unsubscribe(OnObstacleTriggered);
        }

        private void RecordSnapshot(Fruit fruit)
        {
            _snapshotPos = fruit.GridPosition;
            _snapshotFruitType = fruit.Color;
            _pendingObstacleUndos.Clear();
            _hasSnapshot = true;
            if (_debugLogging)
                Debug.Log($"[#{++_seq}][UndoBooster] RecordSnapshot → pos={_snapshotPos} tip={_snapshotFruitType}");
        }

        private void OnObstacleTriggered(ObstacleTriggerPayload payload)
        {
            if (!_hasSnapshot) return;
            _pendingObstacleUndos.Add(payload.UndoAction);
            if (_debugLogging)
                Debug.Log($"[#{++_seq}][UndoBooster] ObstacleUndo eklendi → pos={payload.Position} (toplam: {_pendingObstacleUndos.Count})");
        }

        public bool Execute(CarMatchClone.Board.Board board, GameState state)
        {
            if (_debugLogging)
                Debug.Log($"[#{++_seq}][UndoBooster] Execute → hasSnapshot={_hasSnapshot}, obstacleCount={_pendingObstacleUndos.Count}, pos={_snapshotPos}, tip={_snapshotFruitType}");

            if (!_hasSnapshot)
            {
                Debug.LogWarning("[UndoBooster] Geri alınacak hamle yok.");
                return false;
            }

            // 1. Obstacle yan etkilerini ters sırayla geri al.
            if (_debugLogging) Debug.Log($"[#{++_seq}][UndoBooster] Adım 1 — {_pendingObstacleUndos.Count} obstacle undo");
            for (int i = _pendingObstacleUndos.Count - 1; i >= 0; i--)
                _pendingObstacleUndos[i]?.Invoke();
            _pendingObstacleUndos.Clear();

            // 2. Holder'dan son eklenen meyveyi çıkar.
            if (_debugLogging) Debug.Log($"[#{++_seq}][UndoBooster] Adım 2 — TryRemoveLastAdded");
            if (!_holder.TryRemoveLastAdded())
            {
                Debug.LogWarning("[UndoBooster] Holder'dan meyve çıkarılamadı (eşleşmiş veya boş).");
                _hasSnapshot = false;
                return false;
            }
            if (_debugLogging) Debug.Log($"[#{++_seq}][UndoBooster] Adım 2 — başarılı");

            // 3. Orijinal meyveyi board'a geri koy.
            if (_debugLogging) Debug.Log($"[#{++_seq}][UndoBooster] Adım 3 — PlaceFruitBack pos={_snapshotPos} tip={_snapshotFruitType}");
            bool placed = board.PlaceFruitBack(_snapshotPos, _snapshotFruitType);
            if (_debugLogging) Debug.Log($"[#{++_seq}][UndoBooster] Adım 3 — PlaceFruitBack sonuç={placed}");

            if (!placed)
                Debug.LogWarning($"[UndoBooster] PlaceFruitBack başarısız — {_snapshotPos} hücresi dolu veya mevcut değil.");

            _hasSnapshot = false;
            return placed;
        }
    }
}
