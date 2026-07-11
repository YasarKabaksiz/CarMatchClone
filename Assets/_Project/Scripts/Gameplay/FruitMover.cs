using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Gameplay
{
    public class FruitMover : MonoBehaviour
    {
        [SerializeField] private float _stepDuration  = 0.2f;
        [SerializeField] private float _rollingRadius = 0.5f;  // büyük değer = yavaş dönüş
        [SerializeField] private FruitEventChannel _onFruitSelectedChannel;
        [SerializeField] private FruitEventChannel _onFruitReachedHolderChannel;

        private Fruit _fruit;
        private CarMatchClone.Board.Board _board;
        private CarMatchClone.Board.PathfindingService _pathfindingService;
        private Transform _holderEntryPoint;
        private float _meshCenterY; // BoxCollider.center.y × lossyScale.y — mesh geometrik merkezi yüksekliği
        private Vector3 _pathPos;   // DOTween'in sürdüğü sanal yol pozisyonu (Y=0 düzlemi)

        private void Awake()
        {
            _fruit = GetComponent<Fruit>();
            _board = FindFirstObjectByType<CarMatchClone.Board.Board>();
            _pathfindingService = FindFirstObjectByType<CarMatchClone.Board.PathfindingService>();
            var ep = FindFirstObjectByType<HolderEntryPoint>();
            if (ep == null)
                Debug.LogWarning("[FruitMover] HolderEntryPoint sahnede bulunamadı — meyve exit noktasında duracak.");
            _holderEntryPoint = ep != null ? ep.transform : null;

            var col = GetComponent<BoxCollider>();
            _meshCenterY = col != null ? col.center.y * transform.lossyScale.y : 0f;
        }

        private void OnEnable()
        {
            if (_onFruitSelectedChannel == null)
            {
                Debug.LogError($"[FruitMover] OnFruitSelectedChannel atanmamış — {gameObject.name} hareket etmez.");
                return;
            }
            _onFruitSelectedChannel.Subscribe(HandleFruitSelected);
        }

        private void OnDisable()
        {
            _onFruitSelectedChannel?.Unsubscribe(HandleFruitSelected);
        }

        private void HandleFruitSelected(Fruit fruit)
        {
            if (fruit != _fruit) return;

            var gridPath = _pathfindingService.GetPathToExit(_fruit.GridPosition, _board);
            if (gridPath == null || gridPath.Count == 0)
            {
                _onFruitReachedHolderChannel.Raise(_fruit);
                return;
            }

            var worldPath = new List<Vector3>(gridPath.Count + 1);
            foreach (var pos in gridPath)
                worldPath.Add(_board.GridToWorld(pos));
            if (_holderEntryPoint != null)
                worldPath.Add(_holderEntryPoint.position);

            // _pathPos: grid yolu üzerindeki hedef pozisyon (Y=0).
            // transform.position bu'dan şu formülle türetilir:
            //   transform.position = _pathPos + V - q*V   (V = up * _meshCenterY)
            // => mesh_center_world = transform.position + q*V = _pathPos + V   (sabit yükseklik, zemine gömülme yok)
            _pathPos = transform.position;
            Vector3 prevPathPos = _pathPos;

            var sequence = DOTween.Sequence();
            foreach (var wp in worldPath)
            {
                Vector3 capturedWp = wp;
                sequence.Append(
                    DOTween.To(() => _pathPos, x => _pathPos = x, capturedWp, _stepDuration)
                    .SetEase(Ease.Linear));
            }

            sequence.OnUpdate(() =>
            {
                Vector3 delta = _pathPos - prevPathPos;
                if (delta.sqrMagnitude > 1e-6f)
                {
                    Vector3 rollAxis = Vector3.Cross(Vector3.up, delta.normalized);
                    float   angle    = (delta.magnitude / _rollingRadius) * Mathf.Rad2Deg;
                    transform.Rotate(rollAxis, angle, Space.World);
                }
                prevPathPos = _pathPos;

                // Mesh merkezini her frame _pathPos + H yüksekliğinde tut.
                Vector3 centerVec    = Vector3.up * _meshCenterY;
                transform.position   = _pathPos + centerVec - transform.rotation * centerVec;
            });

            sequence.OnComplete(() =>
            {
                transform.rotation = Quaternion.identity;
                transform.position = _pathPos;
                _onFruitReachedHolderChannel.Raise(_fruit);
            });
        }
    }
}
