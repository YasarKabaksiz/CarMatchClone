using UnityEngine;
using CarMatchClone.Core;
using CarMatchClone.Core.Events;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Boosters
{
    public class SuperUndoBooster : MonoBehaviour, IBooster
    {
        [SerializeField] private CarEventChannel _onCarSelectedChannel;
        [SerializeField] private Holder _holder;
        [SerializeField] private Transform _reserveSlotTransform;

        private bool _isActive;
        private Car _reservedCar;

        public bool HasReservedCar => _reservedCar != null;

        public bool Execute(CarMatchClone.Board.Board board, GameState state)
        {
            if (_isActive)
            {
                Debug.LogWarning("[SuperUndoBooster] Zaten aktif — önce mevcut araç seçilmeli.");
                return false;
            }
            if (_reservedCar != null)
            {
                Debug.LogWarning("[SuperUndoBooster] Rezerv slotta araç var — önce ReleaseReserve çağrılmalı.");
                return false;
            }

            _isActive = true;
            _holder.SetNextCarInterceptor(PlaceInReserve);
            return true;
        }

        private void PlaceInReserve(Car car)
        {
            _isActive = false;
            _reservedCar = car;

            // Rezervdeki araç input sisteminden tamamen izole edilmeli:
            // IsReachable=false → GameInputHandler tıklamayı reddeder.
            // Collider disabled → Raycast'e hiç yakalanmaz (çift koruma).
            car.IsReachable = false;
            var col = car.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (_reserveSlotTransform != null)
                car.transform.position = _reserveSlotTransform.position;
        }

        public void ReleaseReserve()
        {
            if (_reservedCar == null)
            {
                Debug.LogWarning("[SuperUndoBooster] Rezerv slotta araç yok.");
                return;
            }

            var car = _reservedCar;
            _reservedCar = null;

            // Collider'ı geri aç; ForceAddCar zaten IsReachable=false yapar.
            // Pool'a geri döndüğünde collider'ın enabled=true olması gerekir.
            var col = car.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            _holder.ForceAddCar(car);
        }
    }
}
