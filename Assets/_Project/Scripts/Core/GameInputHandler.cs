using UnityEngine;
using UnityEngine.InputSystem;
using CarMatchClone.Core.Events;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core
{
    public class GameInputHandler : MonoBehaviour
    {
        [SerializeField] private CarEventChannel _onCarSelectedChannel;

        private InputAction _clickAction;

        private void OnEnable()
        {
            _clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            _clickAction.performed += OnClick;
            _clickAction.Enable();
        }

        private void OnDisable()
        {
            _clickAction.performed -= OnClick;
            _clickAction.Disable();
            _clickAction.Dispose();
        }

        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (Mouse.current == null) return;

            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var car = hit.collider.GetComponent<Car>();
                if (car != null && car.IsReachable)
                    _onCarSelectedChannel.Raise(car);
            }
        }
    }
}
