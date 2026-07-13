using UnityEngine;
using UnityEngine.InputSystem;
using CarMatchClone.Core.Events;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core
{
    public class GameInputHandler : MonoBehaviour
    {
        [SerializeField] private FruitEventChannel  _onFruitSelectedChannel;
        [SerializeField] private FruitEventChannel  _onFruitReachedHolderChannel;
        [SerializeField] private VoidEventChannel _onHolderProcessedChannel;
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;
        [SerializeField] private VoidEventChannel _onNewLevelLoadedChannel;

        private InputAction _clickAction;
        private bool _moveLocked;
        private bool _inputLocked;

        private void OnEnable()
        {
            _clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            _clickAction.performed += OnClick;
            _clickAction.Enable();
            _onFruitReachedHolderChannel.Subscribe(HandleFruitReachedHolder);
            _onHolderProcessedChannel?.Subscribe(HandleHolderProcessed);
            _onGameOverChannel.Subscribe(HandleGameOver);
            _onLevelCompleteChannel.Subscribe(HandleLevelComplete);
            _onNewLevelLoadedChannel.Subscribe(HandleNewLevelLoaded);
        }

        private void OnDisable()
        {
            _clickAction.performed -= OnClick;
            _clickAction.Disable();
            _clickAction.Dispose();
            _onFruitReachedHolderChannel.Unsubscribe(HandleFruitReachedHolder);
            _onHolderProcessedChannel?.Unsubscribe(HandleHolderProcessed);
            _onGameOverChannel.Unsubscribe(HandleGameOver);
            _onLevelCompleteChannel.Unsubscribe(HandleLevelComplete);
            _onNewLevelLoadedChannel.Unsubscribe(HandleNewLevelLoaded);
        }

        private void HandleFruitReachedHolder(Fruit fruit) => _moveLocked = false;
        private void HandleHolderProcessed()               => _moveLocked = false;
        private void HandleGameOver()                       => _inputLocked = true;
        private void HandleLevelComplete()                  => _inputLocked = true;
        private void HandleNewLevelLoaded()                 { _inputLocked = false; _moveLocked = false; }

        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (_inputLocked || _moveLocked || Mouse.current == null) return;

            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            var fruit = hit.collider.GetComponent<Fruit>();
            if (fruit != null && fruit.IsReachable)
            {
                _moveLocked = true;
                _onFruitSelectedChannel.Raise(fruit);
            }
        }
    }
}
