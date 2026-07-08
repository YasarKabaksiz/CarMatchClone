using UnityEngine;
using UnityEngine.InputSystem;
using CarMatchClone.Core.Events;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core
{
    public class GameInputHandler : MonoBehaviour
    {
        [SerializeField] private CarEventChannel _onCarSelectedChannel;
        [SerializeField] private CarEventChannel _onCarReachedHolderChannel;

        private InputAction _clickAction;
        private bool _moveLocked;

        private void OnEnable()
        {
            _clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            _clickAction.performed += OnClick;
            _clickAction.Enable();
            _onCarReachedHolderChannel.Subscribe(HandleCarReachedHolder);
        }

        private void OnDisable()
        {
            _clickAction.performed -= OnClick;
            _clickAction.Disable();
            _clickAction.Dispose();
            _onCarReachedHolderChannel.Unsubscribe(HandleCarReachedHolder);
        }

        private void HandleCarReachedHolder(Car car)
        {
            _moveLocked = false;
        }

        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (_moveLocked || Mouse.current == null) return;

            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var car = hit.collider.GetComponent<Car>();
                if (car != null && car.IsReachable)
                {
                    _moveLocked = true;
                    _onCarSelectedChannel.Raise(car);
                }
            }
        }
    }
}
