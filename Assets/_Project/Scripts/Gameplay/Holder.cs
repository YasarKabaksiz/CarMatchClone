using UnityEngine;
using CarMatchClone.Core.Events;
using CarMatchClone.Core.Pooling;
using CarMatchClone.Data;

namespace CarMatchClone.Gameplay
{
    public class Holder : MonoBehaviour
    {
        [SerializeField] private int _maxSlots = 7;
        [SerializeField] private Transform[] _slotTransforms;
        [SerializeField] private Transform _entryPoint;
        [SerializeField] private CarEventChannel _onCarReachedHolderChannel;
        [SerializeField] private ColorEventChannel _onMatchOccurredChannel;
        [SerializeField] private VoidEventChannel _onHolderFullChannel;
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private ObjectPoolManager _poolManager;

        private Car[] _slots;
        private Car _lastAddedCar;
        private System.Action<Car> _nextCarInterceptor;

        public bool IsFull
        {
            get
            {
                for (int i = 0; i < _maxSlots; i++)
                    if (_slots[i] == null) return false;
                return true;
            }
        }

        private void Awake()
        {
            _slots = new Car[_maxSlots];
            if (_slotTransforms.Length != _maxSlots)
                Debug.LogError($"[Holder] SlotTransforms sayısı ({_slotTransforms.Length}) maxSlots ({_maxSlots}) ile eşleşmiyor.");
        }

        private void OnEnable()
        {
            _onCarReachedHolderChannel.Subscribe(HandleCarReachedHolder);
        }

        private void OnDisable()
        {
            _onCarReachedHolderChannel.Unsubscribe(HandleCarReachedHolder);
        }

        private void HandleCarReachedHolder(Car car)
        {
            // SuperUndoBooster bir sonraki aracı yakalar; normal akış atlanır.
            if (_nextCarInterceptor != null)
            {
                var interceptor = _nextCarInterceptor;
                _nextCarInterceptor = null;
                interceptor(car);
                return;
            }

            car.IsReachable = false;

            if (IsFull)
            {
                _onHolderFullChannel.Raise();
                _onGameOverChannel.Raise();
                return;
            }

            InsertIntoSlot(car);
            SnapAllCars();
            ResolveMatches();

            if (IsFull)
            {
                _onHolderFullChannel.Raise();
                _onGameOverChannel.Raise();
            }
        }

        private void InsertIntoSlot(Car car)
        {
            int insertAt = -1;

            // Find last slot containing same-color car; insert after it.
            for (int i = _maxSlots - 1; i >= 0; i--)
            {
                if (_slots[i] != null && _slots[i].Color == car.Color)
                {
                    insertAt = i + 1;
                    break;
                }
            }

            // No same-color car found; use first empty slot.
            if (insertAt < 0)
            {
                for (int i = 0; i < _maxSlots; i++)
                {
                    if (_slots[i] == null) { insertAt = i; break; }
                }
            }

            if (insertAt < 0 || insertAt >= _maxSlots) return;

            // Find first null at or after insertAt to shift into.
            int firstNull = -1;
            for (int i = insertAt; i < _maxSlots; i++)
            {
                if (_slots[i] == null) { firstNull = i; break; }
            }

            if (firstNull < 0) return;

            for (int i = firstNull; i > insertAt; i--)
                _slots[i] = _slots[i - 1];

            _slots[insertAt] = car;
            _lastAddedCar = car;
        }

        private void ResolveMatches()
        {
            int matchStart;
            while ((matchStart = MatchChecker.FindMatch(_slots)) >= 0)
            {
                CarColor color = _slots[matchStart].Color;
                for (int i = matchStart; i < matchStart + 3; i++)
                {
                    _poolManager.Release(_slots[i].SourcePrefab, _slots[i].gameObject);
                    _slots[i] = null;
                }
                CompactSlots();
                SnapAllCars();
                _onMatchOccurredChannel.Raise(color);
            }
        }

        private void CompactSlots()
        {
            int write = 0;
            for (int read = 0; read < _maxSlots; read++)
            {
                if (_slots[read] != null)
                    _slots[write++] = _slots[read];
            }
            for (int i = write; i < _maxSlots; i++)
                _slots[i] = null;
        }

        private void SnapAllCars()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null)
                    _slots[i].transform.position = _slotTransforms[i].position;
            }
        }

        // UndoBooster: en son eklenen aracı holder'dan çıkarır ve pool'a bırakır.
        public bool TryRemoveLastAdded()
        {
            if (_lastAddedCar == null) return false;

            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] == _lastAddedCar)
                {
                    _poolManager.Release(_lastAddedCar.SourcePrefab, _lastAddedCar.gameObject);
                    _slots[i] = null;
                    _lastAddedCar = null;
                    CompactSlots();
                    SnapAllCars();
                    return true;
                }
            }

            _lastAddedCar = null;
            return false;
        }

        // SuperUndoBooster: bir sonraki holder'a gelen aracı yakalar; tek seferlik.
        public void SetNextCarInterceptor(System.Action<Car> interceptor)
        {
            _nextCarInterceptor = interceptor;
        }

        // MagnetBooster: holder'daki dolu slotların renklerini döndürür.
        public CarMatchClone.Data.CarColor[] GetOccupiedColors()
        {
            var result = new System.Collections.Generic.List<CarMatchClone.Data.CarColor>();
            for (int i = 0; i < _maxSlots; i++)
                if (_slots[i] != null) result.Add(_slots[i].Color);
            return result.ToArray();
        }

        // SuperUndoBooster: rezerv slottan geri gelen aracı normal akışla holder'a ekler.
        public void ForceAddCar(Car car)
        {
            if (IsFull) return;
            car.IsReachable = false;
            InsertIntoSlot(car);
            SnapAllCars();
            ResolveMatches();
            if (IsFull)
            {
                _onHolderFullChannel.Raise();
                _onGameOverChannel.Raise();
            }
        }

        public Bounds GetBounds()
        {
            if (_slotTransforms == null || _slotTransforms.Length == 0)
                return new Bounds(transform.position, Vector3.zero);
            var b = new Bounds(_slotTransforms[0].position, Vector3.zero);
            for (int i = 1; i < _slotTransforms.Length; i++)
                b.Encapsulate(_slotTransforms[i].position);
            return b;
        }
    }
}
