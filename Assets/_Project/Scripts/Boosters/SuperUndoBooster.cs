using UnityEngine;
using CarMatchClone.Core;
using CarMatchClone.Core.Events;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Boosters
{
    public class SuperUndoBooster : MonoBehaviour, IBooster
    {
        [SerializeField] private FruitEventChannel _onFruitSelectedChannel;
        [SerializeField] private Holder _holder;
        [SerializeField] private Transform _reserveSlotTransform;

        private bool _isActive;
        private Fruit _reservedFruit;

        public bool HasReservedFruit => _reservedFruit != null;

        public bool Execute(CarMatchClone.Board.Board board, GameState state)
        {
            if (_isActive)
            {
                Debug.LogWarning("[SuperUndoBooster] Zaten aktif — önce mevcut meyve seçilmeli.");
                return false;
            }
            if (_reservedFruit != null)
            {
                Debug.LogWarning("[SuperUndoBooster] Rezerv slotta meyve var — önce ReleaseReserve çağrılmalı.");
                return false;
            }

            _isActive = true;
            _holder.SetNextFruitInterceptor(PlaceInReserve);
            return true;
        }

        private void PlaceInReserve(Fruit fruit)
        {
            _isActive = false;
            _reservedFruit = fruit;

            // Rezervdeki meyve input sisteminden tamamen izole edilmeli:
            // IsReachable=false → GameInputHandler tıklamayı reddeder.
            // Collider disabled → Raycast'e hiç yakalanmaz (çift koruma).
            fruit.IsReachable = false;
            var col = fruit.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (_reserveSlotTransform != null)
                fruit.transform.position = _reserveSlotTransform.position;
        }

        public void ReleaseReserve()
        {
            if (_reservedFruit == null)
            {
                Debug.LogWarning("[SuperUndoBooster] Rezerv slotta meyve yok.");
                return;
            }

            var fruit = _reservedFruit;
            _reservedFruit = null;

            // Collider'ı geri aç; ForceAddFruit zaten IsReachable=false yapar.
            // Pool'a geri döndüğünde collider'ın enabled=true olması gerekir.
            var col = fruit.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            _holder.ForceAddFruit(fruit);
        }
    }
}
