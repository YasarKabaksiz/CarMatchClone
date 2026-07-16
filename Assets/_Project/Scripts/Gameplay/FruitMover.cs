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

        [Header("Slot Zıplama")]
        [SerializeField] private float _jumpHeight        = 0.4f;
        [SerializeField] private float _lastStepDuration  = 0.4f;

        [Header("Hareket VFX")]
        [SerializeField] private GameObject _smokeTrailPrefab;
        [SerializeField] private float _smokeStartSize     = 0.15f;
        [SerializeField] private float _smokeStartSpeed    = 0f;
        [SerializeField] private float _smokeStartLifetime = 0.35f;
        [SerializeField] private float _smokeEmissionRate  = 40f;

        private float       _jumpYOffset;
        private GameObject  _activeSmokeTrail;

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
            if (_activeSmokeTrail != null)
            {
                _activeSmokeTrail.transform.SetParent(null);
                var ps = _activeSmokeTrail.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop();
                _activeSmokeTrail = null;
            }
        }

        private void HandleSlotAssigned(SlotAssignedPayload payload)
        {
            if (payload.Fruit != _fruit) return;

            if (_smokeTrailPrefab != null)
            {
                _activeSmokeTrail = Instantiate(_smokeTrailPrefab, transform.position, Quaternion.identity, transform);
                var ps = _activeSmokeTrail.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.startSize       = new ParticleSystem.MinMaxCurve(_smokeStartSize);
                    main.startSpeed      = new ParticleSystem.MinMaxCurve(_smokeStartSpeed);
                    main.startLifetime   = new ParticleSystem.MinMaxCurve(_smokeStartLifetime);
                    var emission = ps.emission;
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(_smokeEmissionRate);
                    ps.Play();
                }
            }

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

            _jumpYOffset = 0f;
            _sequence?.Kill();
            _sequence = DOTween.Sequence();
            for (int i = 0; i < worldPath.Count; i++)
            {
                Vector3 capturedWp = worldPath[i];
                bool    isLast     = i == worldPath.Count - 1;
                float   dur        = isLast ? _lastStepDuration : _stepDuration;

                _sequence.Append(
                    DOTween.To(() => _pathPos, x => _pathPos = x, capturedWp, dur)
                    .SetEase(Ease.Linear));

                if (isLast && _jumpHeight > 0f)
                {
                    float half = dur * 0.5f;
                    _sequence.Join(
                        DOTween.Sequence()
                            .Append(DOTween.To(() => _jumpYOffset, y => _jumpYOffset = y, _jumpHeight, half).SetEase(Ease.OutQuad))
                            .Append(DOTween.To(() => _jumpYOffset, y => _jumpYOffset = y, 0f,          half).SetEase(Ease.InQuad))
                    );
                }
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
                transform.position = _pathPos + Vector3.up * _jumpYOffset + centerVec - transform.rotation * centerVec;
            });

            _sequence.OnComplete(() =>
            {
                _jumpYOffset = 0f;
                if (_activeSmokeTrail != null)
                {
                    _activeSmokeTrail.transform.SetParent(null);
                    var ps = _activeSmokeTrail.GetComponent<ParticleSystem>();
                    if (ps != null) ps.Stop();
                    _activeSmokeTrail = null;
                }
                transform.rotation = Quaternion.identity;
                transform.position = payload.Position;
                _onFruitReachedHolderChannel.Raise(_fruit);
            });
        }
    }
}
