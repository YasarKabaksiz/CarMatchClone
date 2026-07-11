using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Core;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Boosters
{
    public class MagnetBooster : MonoBehaviour, IBooster
    {
        [SerializeField] private FruitEventChannel _onFruitSelectedChannel;
        [SerializeField] private FruitEventChannel _onFruitReachedHolderChannel;
        [SerializeField] private Holder _holder;
        [SerializeField] private bool _debugLogging;

        private readonly Queue<Fruit> _magnetQueue = new Queue<Fruit>();
        private readonly HashSet<Fruit> _inFlightFruits = new HashSet<Fruit>();

        private static int _seq;

        private void OnEnable()
        {
            if (_debugLogging)
                Debug.Log($"[MagnetBooster] OnEnable — channel={(object)_onFruitReachedHolderChannel ?? "NULL"} (name={_onFruitReachedHolderChannel?.name})");
            _onFruitReachedHolderChannel?.Subscribe(OnMagnetFruitReachedHolder);
        }

        private void OnDisable()
        {
            _onFruitReachedHolderChannel?.Unsubscribe(OnMagnetFruitReachedHolder);
        }

        public bool Execute(CarMatchClone.Board.Board board, GameState state)
        {
            if (_inFlightFruits.Count > 0 || _magnetQueue.Count > 0)
            {
                if (_debugLogging)
                    Debug.LogWarning($"[MagnetBooster] Execute reddedildi — önceki sıra bitmedi (inFlight={_inFlightFruits.Count}, queue={_magnetQueue.Count})");
                return false;
            }

            var candidates = GetCandidateFruitTypes(board);

            if (_debugLogging)
            {
                var sb = new System.Text.StringBuilder("[MagnetBooster] Aday tipler: ");
                foreach (var c in candidates) sb.Append(c).Append(' ');
                Debug.Log(sb.ToString());
            }

            foreach (var fruitType in candidates)
            {
                var targets = GetReachableTargets(board, fruitType);
                if (_debugLogging)
                    Debug.Log($"[MagnetBooster] {fruitType} → erişilebilir: {targets.Count}");
                if (targets.Count == 0) continue;

                foreach (var fruit in targets)
                {
                    _magnetQueue.Enqueue(fruit);
                    _inFlightFruits.Add(fruit);
                    if (_debugLogging)
                        Debug.Log($"[#{++_seq}][MagnetBooster] Kuyruğa eklendi: {fruit.name} (GetHashCode={fruit.GetHashCode()})");
                }

                if (_debugLogging)
                    Debug.Log($"[#{++_seq}][MagnetBooster] Sıra başlatıldı: {_magnetQueue.Count} meyve, tip={fruitType}");

                SendNext();
                return true;
            }

            Debug.LogWarning("[MagnetBooster] Hiçbir tipte erişilebilir meyve bulunamadı — stok harcanmıyor.");
            return false;
        }

        private void OnMagnetFruitReachedHolder(Fruit fruit)
        {
            if (_debugLogging)
                Debug.Log($"[#{++_seq}][MagnetBooster] OnFruitReachedHolder — fruit={(fruit != null ? fruit.name : "NULL")}, hash={fruit?.GetHashCode()}, inFlightCount={_inFlightFruits.Count}, contains={_inFlightFruits.Contains(fruit)}");

            if (!_inFlightFruits.Remove(fruit))
            {
                if (_debugLogging)
                    Debug.Log($"[#{++_seq}][MagnetBooster] Filtre: bu meyve bizim sıramızda değil, yoksayılıyor.");
                return;
            }

            if (_debugLogging)
                Debug.Log($"[#{++_seq}][MagnetBooster] Meyve tanındı → kalan sıra: {_magnetQueue.Count}");
            SendNext();
        }

        private void SendNext()
        {
            if (_magnetQueue.Count == 0)
            {
                if (_debugLogging)
                    Debug.Log($"[#{++_seq}][MagnetBooster] Sıra tamamlandı.");
                return;
            }
            var next = _magnetQueue.Dequeue();
            if (_debugLogging)
                Debug.Log($"[#{++_seq}][MagnetBooster] SendNext → {next.name} (hash={next.GetHashCode()}), kalan kuyruk={_magnetQueue.Count}");
            _onFruitSelectedChannel.Raise(next);
        }

        private List<FruitType> GetCandidateFruitTypes(CarMatchClone.Board.Board board)
        {
            var holderColors = _holder.GetOccupiedColors();
            Dictionary<FruitType, int> counts;

            if (holderColors.Length > 0)
            {
                counts = new Dictionary<FruitType, int>();
                foreach (var c in holderColors)
                {
                    if (!counts.ContainsKey(c)) counts[c] = 0;
                    counts[c]++;
                }
            }
            else
            {
                counts = new Dictionary<FruitType, int>();
                foreach (var cell in board.GetAllCells())
                {
                    if (cell.Occupant != null && cell.Occupant.IsReachable)
                    {
                        var c = cell.Occupant.Color;
                        if (!counts.ContainsKey(c)) counts[c] = 0;
                        counts[c]++;
                    }
                }
            }

            var sorted = new List<FruitType>(counts.Keys);
            sorted.Sort((a, b) => counts[b].CompareTo(counts[a]));
            return sorted;
        }

        private List<Fruit> GetReachableTargets(CarMatchClone.Board.Board board, FruitType fruitType)
        {
            var result = new List<Fruit>();
            foreach (var cell in board.GetAllCells())
            {
                if (cell.Occupant != null
                    && cell.Occupant.Color == fruitType
                    && cell.Occupant.IsReachable)
                {
                    result.Add(cell.Occupant);
                }
            }
            return result;
        }
    }
}
