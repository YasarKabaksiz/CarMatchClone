using UnityEngine;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;
        [SerializeField] private bool _debugLogging;

        private GameState _gameState;

        private void Awake()
        {
            _gameState = new GameState();
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
