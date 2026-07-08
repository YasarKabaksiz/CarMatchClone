using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Gameplay
{
    public class CarMover : MonoBehaviour
    {
        [SerializeField] private float _stepDuration = 0.2f;
        [SerializeField] private CarEventChannel _onCarSelectedChannel;
        [SerializeField] private CarEventChannel _onCarReachedHolderChannel;

        private Car _car;
        private CarMatchClone.Board.Board _board;
        private CarMatchClone.Board.PathfindingService _pathfindingService;
        private Transform _holderEntryPoint;

        private void Awake()
        {
            _car = GetComponent<Car>();
            _board = FindFirstObjectByType<CarMatchClone.Board.Board>();
            _pathfindingService = FindFirstObjectByType<CarMatchClone.Board.PathfindingService>();
            var ep = FindFirstObjectByType<HolderEntryPoint>();
            if (ep == null)
                Debug.LogWarning("[CarMover] HolderEntryPoint sahnede bulunamadı — araç exit noktasında duracak.");
            _holderEntryPoint = ep != null ? ep.transform : null;
        }

        private void OnEnable()
        {
            _onCarSelectedChannel.Subscribe(HandleCarSelected);
        }

        private void OnDisable()
        {
            _onCarSelectedChannel.Unsubscribe(HandleCarSelected);
        }

        private void HandleCarSelected(Car car)
        {
            if (car != _car) return;

            var gridPath = _pathfindingService.GetPathToExit(_car.GridPosition, _board);
            if (gridPath == null || gridPath.Count == 0)
            {
                _onCarReachedHolderChannel.Raise(_car);
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
            sequence.OnComplete(() => _onCarReachedHolderChannel.Raise(_car));
        }
    }
}
