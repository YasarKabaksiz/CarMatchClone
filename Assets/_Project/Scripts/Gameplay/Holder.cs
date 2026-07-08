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
        [SerializeField] private CarEventChannel _onCarSelectedChannel;
        [SerializeField] private ColorEventChannel _onMatchOccurredChannel;
        [SerializeField] private VoidEventChannel _onHolderFullChannel;
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private ObjectPoolManager _poolManager;
        [SerializeField] private GameObject _carPrefab;

        private Car[] _slots;

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
            _onCarSelectedChannel.Subscribe(HandleCarSelected);
        }

        private void OnDisable()
        {
            _onCarSelectedChannel.Unsubscribe(HandleCarSelected);
        }

        private void HandleCarSelected(Car car)
        {
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
        }

        private void ResolveMatches()
        {
            int matchStart;
            while ((matchStart = MatchChecker.FindMatch(_slots)) >= 0)
            {
                CarColor color = _slots[matchStart].Color;
                for (int i = matchStart; i < matchStart + 3; i++)
                {
                    _poolManager.Release(_carPrefab, _slots[i].gameObject);
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
    }
}
