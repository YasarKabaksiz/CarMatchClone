using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Boosters;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;

namespace CarMatchClone.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;
        [SerializeField] private BoosterEventChannel _onBoosterUsedChannel;
        [SerializeField] private bool _debugLogging;

        [Header("Booster Referansları")]
        [SerializeField] private CarMatchClone.Board.Board _board;
        [SerializeField] private UndoBooster _undoBooster;
        [SerializeField] private ShuffleBooster _shuffleBooster;
        [SerializeField] private SuperUndoBooster _superUndoBooster;
        [SerializeField] private MagnetBooster _magnetBooster;

        private GameState _gameState;
        private Dictionary<BoosterType, IBooster> _boosters;

        private void Awake()
        {
            _gameState = new GameState();
            InitBoosterCounts();
            InitBoosterMap();
        }

        private void InitBoosterCounts()
        {
            _gameState.BoosterCounts[BoosterType.Undo] = 3;
            _gameState.BoosterCounts[BoosterType.Shuffle] = 3;
            _gameState.BoosterCounts[BoosterType.SuperUndo] = 3;
            _gameState.BoosterCounts[BoosterType.Magnet] = 3;
        }

        private void InitBoosterMap()
        {
            _boosters = new Dictionary<BoosterType, IBooster>();
            if (_undoBooster != null) _boosters[BoosterType.Undo] = _undoBooster;
            if (_shuffleBooster != null) _boosters[BoosterType.Shuffle] = _shuffleBooster;
            if (_superUndoBooster != null) _boosters[BoosterType.SuperUndo] = _superUndoBooster;
            if (_magnetBooster != null) _boosters[BoosterType.Magnet] = _magnetBooster;
        }

        private void OnEnable()
        {
            _onGameOverChannel.Subscribe(HandleGameOver);
            _onLevelCompleteChannel.Subscribe(HandleLevelComplete);
        }

        private void OnDisable()
        {
            _onGameOverChannel.Unsubscribe(HandleGameOver);
            _onLevelCompleteChannel.Unsubscribe(HandleLevelComplete);
        }

        public void UseBooster(BoosterType type)
        {
            if (_gameState.IsGameOver || _gameState.IsLevelComplete) return;

            if (!_gameState.BoosterCounts.TryGetValue(type, out int count) || count <= 0)
            {
                if (_debugLogging) Debug.LogWarning($"[GameManager] {type} booster stoku tükendi.");
                return;
            }

            if (!_boosters.TryGetValue(type, out var booster) || booster == null)
            {
                if (_debugLogging) Debug.LogWarning($"[GameManager] {type} booster referansı Inspector'da atanmamış.");
                return;
            }

            bool used = booster.Execute(_board, _gameState);
            if (used)
            {
                _gameState.BoosterCounts[type]--;
                _onBoosterUsedChannel?.Raise(type);
            }
        }

        private void HandleGameOver()
        {
            _gameState.IsGameOver = true;
            if (_debugLogging) Debug.Log("[GameManager] Game Over.");
        }

        private void HandleLevelComplete()
        {
            _gameState.IsLevelComplete = true;
            if (_debugLogging) Debug.Log("[GameManager] Level Complete!");
        }
    }
}
