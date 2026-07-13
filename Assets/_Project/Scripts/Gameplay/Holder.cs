using System.Collections.Generic;
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
        [SerializeField] private FruitEventChannel _onFruitSelectedChannel;
        [SerializeField] private FruitEventChannel _onFruitReachedHolderChannel;
        [SerializeField] private FruitSlotEventChannel _onSlotAssignedChannel;
        [SerializeField] private ColorEventChannel _onMatchOccurredChannel;
        [SerializeField] private VoidEventChannel _onHolderFullChannel;
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private VoidEventChannel _onHolderProcessedChannel;
        [SerializeField] private ObjectPoolManager _poolManager;

        private Fruit[] _slots;
        private Fruit _lastAddedFruit;
        private Fruit _transitFruit;
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
            _onFruitSelectedChannel.Subscribe(HandleFruitSelected);
            _onFruitReachedHolderChannel.Subscribe(HandleFruitReachedHolder);
        }

        private void OnDisable()
        {
            _onFruitSelectedChannel?.Unsubscribe(HandleFruitSelected);
            _onFruitReachedHolderChannel.Unsubscribe(HandleFruitReachedHolder);
            _transitFruit = null;
        }

        // Meyve seçildi: slotu hemen rezerve et, tween hedefini yayınla.
        private void HandleFruitSelected(Fruit fruit)
        {
            if (_nextFruitInterceptor != null)
            {
                var interceptor = _nextFruitInterceptor;
                _nextFruitInterceptor = null;
                interceptor(fruit);
                // Tween hiç başlamadı — OnFruitReachedHolder asla fırlamaz.
                // _moveLocked açmak için OnHolderProcessed'ı biz yükseltiyoruz.
                _onHolderProcessedChannel?.Raise();
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
            _transitFruit = fruit;

            int slotIndex = -1;
            for (int i = 0; i < _maxSlots; i++)
                if (_slots[i] == fruit) { slotIndex = i; break; }

            if (slotIndex >= 0)
                _onSlotAssignedChannel.Raise(new SlotAssignedPayload
                {
                    Fruit    = fruit,
                    Position = _slotTransforms[slotIndex].position
                });
        }

        // Meyve tween'ini tamamladı: transit bitir, match resolve.
        private void HandleFruitReachedHolder(Fruit fruit)
        {
            bool inSlots = false;
            for (int i = 0; i < _maxSlots; i++)
                if (_slots[i] == fruit) { inSlots = true; break; }
            if (!inSlots) return;

            _transitFruit = null;
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

            for (int i = _maxSlots - 1; i >= 0; i--)
            {
                if (_slots[i] != null && _slots[i].Color == fruit.Color)
                {
                    insertAt = i + 1;
                    break;
                }
            }

            if (insertAt < 0)
            {
                for (int i = 0; i < _maxSlots; i++)
                {
                    if (_slots[i] == null) { insertAt = i; break; }
                }
            }

            if (insertAt < 0 || insertAt >= _maxSlots) return;

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
                if (_slots[i] != null && _slots[i] != _transitFruit)
                    _slots[i].transform.position = _slotTransforms[i].position;
            }
        }

        public bool TryRemoveLastAdded()
        {
            if (_lastAddedFruit == null) return false;

            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] == _lastAddedFruit)
                {
                    if (_transitFruit == _lastAddedFruit) _transitFruit = null;
                    _poolManager.Release(_lastAddedFruit.SourcePrefab, _lastAddedFruit.gameObject);
                    _slots[i] = null;
                    _lastAddedFruit = null;
                    CompactSlots();
                    SnapAllFruits();
                    // Tween mid-flight'ta öldürüldüyse OnFruitReachedHolder fırlamaz.
                    // _moveLocked açmak için OnHolderProcessed'ı biz yükseltiyoruz.
                    _onHolderProcessedChannel?.Raise();
                    return true;
                }
            }

            _lastAddedFruit = null;
            return false;
        }

        public void SetNextFruitInterceptor(System.Action<Fruit> interceptor)
        {
            _nextFruitInterceptor = interceptor;
        }

        public FruitType[] GetOccupiedColors()
        {
            var result = new List<FruitType>();
            for (int i = 0; i < _maxSlots; i++)
                if (_slots[i] != null) result.Add(_slots[i].Color);
            return result.ToArray();
        }

        // ForceAddFruit (SuperUndoBooster): rezervden gelen meyve, instant snap.
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
            _transitFruit = null;
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
