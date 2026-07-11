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
        [SerializeField] private FruitEventChannel _onFruitReachedHolderChannel;
        [SerializeField] private ColorEventChannel _onMatchOccurredChannel;
        [SerializeField] private VoidEventChannel _onHolderFullChannel;
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private VoidEventChannel _onHolderProcessedChannel;
        [SerializeField] private ObjectPoolManager _poolManager;

        private Fruit[] _slots;
        private Fruit _lastAddedFruit;
        private System.Action<Fruit> _nextFruitInterceptor;

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
            _slots = new Fruit[_maxSlots];
            if (_slotTransforms.Length != _maxSlots)
                Debug.LogError($"[Holder] SlotTransforms sayısı ({_slotTransforms.Length}) maxSlots ({_maxSlots}) ile eşleşmiyor.");
        }

        private void OnEnable()
        {
            _onFruitReachedHolderChannel.Subscribe(HandleFruitReachedHolder);
        }

        private void OnDisable()
        {
            _onFruitReachedHolderChannel.Unsubscribe(HandleFruitReachedHolder);
        }

        private void HandleFruitReachedHolder(Fruit fruit)
        {
            // SuperUndoBooster bir sonraki meyveyi yakalar; normal akış atlanır.
            if (_nextFruitInterceptor != null)
            {
                var interceptor = _nextFruitInterceptor;
                _nextFruitInterceptor = null;
                interceptor(fruit);
                return;
            }

            fruit.IsReachable = false;

            if (IsFull)
            {
                _onHolderFullChannel.Raise();
                _onGameOverChannel.Raise();
                return;
            }

            InsertIntoSlot(fruit);
            SnapAllFruits();
            ResolveMatches();

            if (IsFull)
            {
                _onHolderFullChannel.Raise();
                _onGameOverChannel.Raise();
                return;
            }

            _onHolderProcessedChannel?.Raise();
        }

        private void InsertIntoSlot(Fruit fruit)
        {
            int insertAt = -1;

            // Find last slot containing same-color fruit; insert after it.
            for (int i = _maxSlots - 1; i >= 0; i--)
            {
                if (_slots[i] != null && _slots[i].Color == fruit.Color)
                {
                    insertAt = i + 1;
                    break;
                }
            }

            // No same-color fruit found; use first empty slot.
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

            _slots[insertAt] = fruit;
            _lastAddedFruit = fruit;
        }

        private void ResolveMatches()
        {
            int matchStart;
            while ((matchStart = MatchChecker.FindMatch(_slots)) >= 0)
            {
                FruitType fruitType = _slots[matchStart].Color;
                for (int i = matchStart; i < matchStart + 3; i++)
                {
                    _poolManager.Release(_slots[i].SourcePrefab, _slots[i].gameObject);
                    _slots[i] = null;
                }
                CompactSlots();
                SnapAllFruits();
                _onMatchOccurredChannel.Raise(fruitType);
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

        private void SnapAllFruits()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null)
                    _slots[i].transform.position = _slotTransforms[i].position;
            }
        }

        // UndoBooster: en son eklenen meyveyi holder'dan çıkarır ve pool'a bırakır.
        public bool TryRemoveLastAdded()
        {
            if (_lastAddedFruit == null) return false;

            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] == _lastAddedFruit)
                {
                    _poolManager.Release(_lastAddedFruit.SourcePrefab, _lastAddedFruit.gameObject);
                    _slots[i] = null;
                    _lastAddedFruit = null;
                    CompactSlots();
                    SnapAllFruits();
                    return true;
                }
            }

            _lastAddedFruit = null;
            return false;
        }

        // SuperUndoBooster: bir sonraki holder'a gelen meyveyi yakalar; tek seferlik.
        public void SetNextFruitInterceptor(System.Action<Fruit> interceptor)
        {
            _nextFruitInterceptor = interceptor;
        }

        // MagnetBooster: holder'daki dolu slotların tiplerini döndürür.
        public FruitType[] GetOccupiedColors()
        {
            var result = new System.Collections.Generic.List<FruitType>();
            for (int i = 0; i < _maxSlots; i++)
                if (_slots[i] != null) result.Add(_slots[i].Color);
            return result.ToArray();
        }

        // SuperUndoBooster: rezerv slottan geri gelen meyveyi normal akışla holder'a ekler.
        public void ForceAddFruit(Fruit fruit)
        {
            if (IsFull) return;
            fruit.IsReachable = false;
            InsertIntoSlot(fruit);
            SnapAllFruits();
            ResolveMatches();
            if (IsFull)
            {
                _onHolderFullChannel.Raise();
                _onGameOverChannel.Raise();
                return;
            }
            _onHolderProcessedChannel?.Raise();
        }

        // Retry/Level Complete: tüm slotları boşaltır, meyveleri pool'a iade eder.
        public void ClearAllSlots()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null)
                {
                    _poolManager.Release(_slots[i].SourcePrefab, _slots[i].gameObject);
                    _slots[i] = null;
                }
            }
            _lastAddedFruit = null;
            _nextFruitInterceptor = null;
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
