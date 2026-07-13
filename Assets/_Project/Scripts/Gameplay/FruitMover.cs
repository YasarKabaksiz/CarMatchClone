using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Gameplay
{
    public class FruitMover : MonoBehaviour
    {
        [SerializeField] private float _stepDuration  = 0.2f;
        [SerializeField] private float _rollingRadius = 0.5f;
        [SerializeField] private FruitSlotEventChannel _onSlotAssignedChannel;
        [SerializeField] private FruitEventChannel _onFruitReachedHolderChannel;

        private Fruit _fruit;
        private CarMatchClone.Board.Board _board;
        private CarMatchClone.Board.PathfindingService _pathfindingService;
        private float _meshCenterY;
        private Vector3 _pathPos;
        private Sequence _sequence;

        private void Awake()
        {
            _fruit = GetComponent<Fruit>();
            _board = FindFirstObjectByType<CarMatchClone.Board.Board>();
            _pathfindingService = FindFirstObjectByType<CarMatchClone.Board.PathfindingService>();

            var col = GetComponent<BoxCollider>();
            _meshCenterY = col != null ? col.center.y * transform.lossyScale.y : 0f;
        }

        private void OnEnable()
        {
            if (_onSlotAssignedChannel == null)
            {
                Debug.LogError($"[FruitMover] OnSlotAssignedChannel atanmamış — {gameObject.name} hareket etmez.");
                return;
            }
            _onSlotAssignedChannel.Subscribe(HandleSlotAssigned);
        }

        private void OnDisable()
        {
            _onSlotAssignedChannel?.Unsubscribe(HandleSlotAssigned);
            _sequence?.Kill();
            _sequence = null;
        }

        private void HandleSlotAssigned(SlotAssignedPayload payload)
        {
            if (payload.Fruit != _fruit) return;

            var gridPath = _pathfindingService.GetPathToExit(_fruit.GridPosition, _board);

            var worldPath = new List<Vector3>();
            if (gridPath != null)
            {
                foreach (var pos in gridPath)
                    worldPath.Add(_board.GridToWorld(pos));
            }
            // Slot pozisyonu son waypoint — HolderEntryPoint kullanılmıyor.
            worldPath.Add(payload.Position);

            _pathPos = transform.position;
            Vector3 prevPathPos = _pathPos;

            _sequence?.Kill();
            _sequence = DOTween.Sequence();
            foreach (var wp in worldPath)
            {
                Vector3 capturedWp = wp;
                _sequence.Append(
                    DOTween.To(() => _pathPos, x => _pathPos = x, capturedWp, _stepDuration)
                    .SetEase(Ease.Linear));
            }

            _sequence.OnUpdate(() =>
            {
                Vector3 delta = _pathPos - prevPathPos;
                if (delta.sqrMagnitude > 1e-6f)
                {
                    Vector3 rollAxis = Vector3.Cross(Vector3.up, delta.normalized);
                    float   angle    = (delta.magnitude / _rollingRadius) * Mathf.Rad2Deg;
                    transform.Rotate(rollAxis, angle, Space.World);
                }
                prevPathPos = _pathPos;

                Vector3 centerVec  = Vector3.up * _meshCenterY;
                transform.position = _pathPos + centerVec - transform.rotation * centerVec;
            });

            _sequence.OnComplete(() =>
            {
                transform.rotation = Quaternion.identity;
                transform.position = payload.Position;
                _onFruitReachedHolderChannel.Raise(_fruit);
            });
        }
    }
}
