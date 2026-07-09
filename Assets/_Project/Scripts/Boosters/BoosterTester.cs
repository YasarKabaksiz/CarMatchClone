// TEST KODU — M9 UI sistemi tamamlaninca bu dosya silinecek.
using UnityEngine;
using UnityEngine.InputSystem;
using CarMatchClone.Core;
using CarMatchClone.Data;

namespace CarMatchClone.Boosters
{
    public class BoosterTester : MonoBehaviour
    {
        [Header("Test - M9'da silinecek")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private SuperUndoBooster _superUndoBooster;

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.uKey.wasPressedThisFrame) _gameManager.UseBooster(BoosterType.Undo);
            if (Keyboard.current.sKey.wasPressedThisFrame) _gameManager.UseBooster(BoosterType.Shuffle);
            if (Keyboard.current.xKey.wasPressedThisFrame) _gameManager.UseBooster(BoosterType.SuperUndo);
            if (Keyboard.current.mKey.wasPressedThisFrame) _gameManager.UseBooster(BoosterType.Magnet);

            if (Keyboard.current.rKey.wasPressedThisFrame && _superUndoBooster != null)
                _superUndoBooster.ReleaseReserve();
        }
    }
}
