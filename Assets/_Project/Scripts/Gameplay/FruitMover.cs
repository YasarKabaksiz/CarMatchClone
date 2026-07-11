using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Gameplay
{
    public class FruitMover : MonoBehaviour
    {
        [SerializeField] private float _stepDuration = 0.2f;
        [SerializeField] private FruitEventChannel _onFruitSelectedChannel;
        [SerializeField] private FruitEventChannel _onFruitReachedHolderChannel;

        private Fruit _fruit;
        private CarMatchClone.Board.Board _board;
        private CarMatchClone.Board.PathfindingService _pathfindingService;
        private Transform _holderEntryPoint;

        private void Awake()
        {
            _fruit = GetComponent<Fruit>();
            _board = FindFirstObjectByType<CarMatchClone.Board.Board>();
            _pathfindingService = FindFirstObjectByType<CarMatchClone.Board.PathfindingService>();
            var ep = FindFirstObjectByType<HolderEntryPoint>();
            if (ep == null)
                Debug.LogWarning("[FruitMover] HolderEntryPoint sahnede bulunamadı — meyve exit noktasında duracak.");
            _holderEntryPoint = ep != null ? ep.transform : null;
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

            var sequence = DOTween.Sequence();
            foreach (var wp in worldPath)
                sequence.Append(transform.DOMove(wp, _stepDuration).SetEase(Ease.Linear));
            sequence.OnComplete(() => _onFruitReachedHolderChannel.Raise(_fruit));
        }
    }
}
