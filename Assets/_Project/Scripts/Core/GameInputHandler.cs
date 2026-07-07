using UnityEngine;
using UnityEngine.InputSystem;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core
{
    public class GameInputHandler : MonoBehaviour
    {
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
                if (car != null)
                    Debug.Log($"[Input] Car at {car.GridPosition} — reachable: {car.IsReachable}");
            }
        }
    }
}
