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
        [SerializeField] private CarEventChannel _onCarSelectedChannel;
        [SerializeField] private CarEventChannel _onCarReachedHolderChannel;
        [SerializeField] private Holder _holder;
        [SerializeField] private bool _debugLogging;

        private readonly Queue<Car> _magnetQueue = new Queue<Car>();
        private readonly HashSet<Car> _inFlightCars = new HashSet<Car>();

        private static int _seq;

        private void OnEnable()
        {
            if (_debugLogging)
                Debug.Log($"[MagnetBooster] OnEnable — channel={(object)_onCarReachedHolderChannel ?? "NULL"} (name={_onCarReachedHolderChannel?.name})");
            _onCarReachedHolderChannel?.Subscribe(OnMagnetCarReachedHolder);
        }

        private void OnDisable()
        {
            _onCarReachedHolderChannel?.Unsubscribe(OnMagnetCarReachedHolder);
        }

        public bool Execute(CarMatchClone.Board.Board board, GameState state)
        {
            if (_inFlightCars.Count > 0 || _magnetQueue.Count > 0)
            {
                if (_debugLogging)
                    Debug.LogWarning($"[MagnetBooster] Execute reddedildi — önceki sıra bitmedi (inFlight={_inFlightCars.Count}, queue={_magnetQueue.Count})");
                return false;
            }

            var candidates = GetCandidateColors(board);

            if (_debugLogging)
            {
                var sb = new System.Text.StringBuilder("[MagnetBooster] Aday renkler: ");
                foreach (var c in candidates) sb.Append(c).Append(' ');
                Debug.Log(sb.ToString());
            }

            foreach (var color in candidates)
            {
                var targets = GetReachableTargets(board, color);
                if (_debugLogging)
                    Debug.Log($"[MagnetBooster] {color} → erişilebilir: {targets.Count}");
                if (targets.Count == 0) continue;

                foreach (var car in targets)
                {
                    _magnetQueue.Enqueue(car);
                    _inFlightCars.Add(car);
                    if (_debugLogging)
                        Debug.Log($"[#{++_seq}][MagnetBooster] Kuyruğa eklendi: {car.name} (GetHashCode={car.GetHashCode()})");
                }

                if (_debugLogging)
                    Debug.Log($"[#{++_seq}][MagnetBooster] Sıra başlatıldı: {_magnetQueue.Count} araç, renk={color}");

                SendNext();
                return true;
            }

            Debug.LogWarning("[MagnetBooster] Hiçbir renkte erişilebilir araç bulunamadı — stok harcanmıyor.");
            return false;
        }

        private void OnMagnetCarReachedHolder(Car car)
        {
            if (_debugLogging)
                Debug.Log($"[#{++_seq}][MagnetBooster] OnCarReachedHolder — car={(car != null ? car.name : "NULL")}, hash={car?.GetHashCode()}, inFlightCount={_inFlightCars.Count}, contains={_inFlightCars.Contains(car)}");

            if (!_inFlightCars.Remove(car))
            {
                if (_debugLogging)
                    Debug.Log($"[#{++_seq}][MagnetBooster] Filtre: bu araç bizim sıramızda değil, yoksayılıyor.");
                return;
            }

            if (_debugLogging)
                Debug.Log($"[#{++_seq}][MagnetBooster] Araç tanındı → kalan sıra: {_magnetQueue.Count}");
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
            _onCarSelectedChannel.Raise(next);
        }

        private List<CarColor> GetCandidateColors(CarMatchClone.Board.Board board)
        {
            var holderColors = _holder.GetOccupiedColors();
            Dictionary<CarColor, int> counts;

            if (holderColors.Length > 0)
            {
                counts = new Dictionary<CarColor, int>();
                foreach (var c in holderColors)
                {
                    if (!counts.ContainsKey(c)) counts[c] = 0;
                    counts[c]++;
                }
            }
            else
            {
                counts = new Dictionary<CarColor, int>();
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

            var sorted = new List<CarColor>(counts.Keys);
            sorted.Sort((a, b) => counts[b].CompareTo(counts[a]));
            return sorted;
        }

        private List<Car> GetReachableTargets(CarMatchClone.Board.Board board, CarColor color)
        {
            var result = new List<Car>();
            foreach (var cell in board.GetAllCells())
            {
                if (cell.Occupant != null
                    && cell.Occupant.Color == color
                    && cell.Occupant.IsReachable)
                {
                    result.Add(cell.Occupant);
                }
            }
            return result;
        }
    }
}
