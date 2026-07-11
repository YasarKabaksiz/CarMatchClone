using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Boosters;
using CarMatchClone.Core.Events;
using CarMatchClone.Core.SaveSystem;
using CarMatchClone.Data;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;
        [SerializeField] private VoidEventChannel _onHolderProcessedChannel;
        [SerializeField] private VoidEventChannel _onNewLevelLoadedChannel;
        [SerializeField] private BoosterEventChannel _onBoosterUsedChannel;
        [SerializeField] private BoosterEventChannel _onBoosterRequestedChannel;
        [SerializeField] private BoosterCountEventChannel _onBoosterCountChangedChannel;
        [SerializeField] private IntEventChannel _onCoinsChangedChannel;
        [SerializeField] private VoidEventChannel _onLevelContinueRequestedChannel;
        [SerializeField] private VoidEventChannel _onRetryRequestedChannel;
        [SerializeField] private bool _debugLogging;

        [Header("Level Sırası")]
        [SerializeField] private LevelData[] _levels;

        [Header("Referanslar")]
        [SerializeField] private CarMatchClone.Board.Board _board;
        [SerializeField] private Holder _holder;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private LevelTransitionData _levelTransitionData;

        [Header("Booster Referansları")]
        [SerializeField] private UndoBooster _undoBooster;
        [SerializeField] private ShuffleBooster _shuffleBooster;
        [SerializeField] private SuperUndoBooster _superUndoBooster;
        [SerializeField] private MagnetBooster _magnetBooster;

        private GameState _gameState;
        private Dictionary<BoosterType, IBooster> _boosters;
        private int _currentLevelIndex;

        private static readonly int[] _defaultBoosterCounts = { 3, 3, 3, 3 };
        private static readonly BoosterType[] _allBoosterTypes =
        {
            BoosterType.Undo, BoosterType.Shuffle, BoosterType.SuperUndo, BoosterType.Magnet
        };

        private void Awake()
        {
            _gameState = new GameState();
            LoadSaveData();
            InitBoosterMap();
        }

        private void Start()
        {
            // Board.Awake() _levelData null ise erken çıkar; burada aktif level'ı yükleriz.
            LoadCurrentLevel();
        }

        private void OnEnable()
        {
            _onGameOverChannel.Subscribe(HandleGameOver);
            _onLevelCompleteChannel.Subscribe(HandleLevelComplete);
            _onHolderProcessedChannel.Subscribe(HandleHolderProcessed);
            _onBoosterRequestedChannel?.Subscribe(HandleBoosterRequested);
            _onLevelContinueRequestedChannel?.Subscribe(HandleLevelContinueRequested);
            _onRetryRequestedChannel?.Subscribe(HandleRetryRequested);
        }

        private void OnDisable()
        {
            _onGameOverChannel.Unsubscribe(HandleGameOver);
            _onLevelCompleteChannel.Unsubscribe(HandleLevelComplete);
            _onHolderProcessedChannel.Unsubscribe(HandleHolderProcessed);
            _onBoosterRequestedChannel?.Unsubscribe(HandleBoosterRequested);
            _onLevelContinueRequestedChannel?.Unsubscribe(HandleLevelContinueRequested);
            _onRetryRequestedChannel?.Unsubscribe(HandleRetryRequested);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveProgress();
        }

        // ── Level yönetimi ──────────────────────────────────────────────────

        private void LoadCurrentLevel()
        {
            if (_levels == null || _levels.Length == 0)
            {
                Debug.LogError("[GameManager] _levels dizisi boş — Inspector'a LevelData ekle.");
                return;
            }

            if (_currentLevelIndex >= _levels.Length)
            {
                if (_debugLogging)
                    Debug.Log("[GameManager] Tüm level'lar tamamlandı.");
                return;
            }

            var level = _levels[_currentLevelIndex];
            if (level == null)
            {
                Debug.LogError($"[GameManager] _levels[{_currentLevelIndex}] null — Inspector'ı kontrol et.");
                return;
            }

            _board.RebuildGrid(level);
            _onNewLevelLoadedChannel?.Raise();
            BroadcastGameState();

            if (_debugLogging)
                Debug.Log($"[GameManager] Level yüklendi: index={_currentLevelIndex}, name={level.name}");
        }

        // ── Booster yönetimi ────────────────────────────────────────────────

        private void InitBoosterMap()
        {
            _boosters = new Dictionary<BoosterType, IBooster>();
            if (_undoBooster != null)    _boosters[BoosterType.Undo]      = _undoBooster;
            if (_shuffleBooster != null) _boosters[BoosterType.Shuffle]   = _shuffleBooster;
            if (_superUndoBooster != null) _boosters[BoosterType.SuperUndo] = _superUndoBooster;
            if (_magnetBooster != null)  _boosters[BoosterType.Magnet]    = _magnetBooster;
        }

        public void UseBooster(BoosterType type)
        {
            if (_gameState.IsGameOver || _gameState.IsLevelComplete) return;

            if (!_gameState.BoosterCounts.TryGetValue(type, out int count) || count <= 0)
            {
                if (_debugLogging) Debug.LogWarning($"[GameManager] {type} booster stoku tükendi.");
                return;
            }

            if (!_boosters.TryGetValue(type, out var booster) || booster == null)
            {
                if (_debugLogging) Debug.LogWarning($"[GameManager] {type} booster referansı Inspector'da atanmamış.");
                return;
            }

            bool used = booster.Execute(_board, _gameState);
            if (used)
            {
                _gameState.BoosterCounts[type]--;
                _onBoosterUsedChannel?.Raise(type);
                _onBoosterCountChangedChannel?.Raise(new BoosterCountPayload
                {
                    Type = type,
                    Count = _gameState.BoosterCounts[type]
                });
                SaveProgress();
            }
        }

        private void HandleBoosterRequested(BoosterType type) => UseBooster(type);

        // ── Event handler'lar ───────────────────────────────────────────────

        private void HandleHolderProcessed()
        {
            if (_gameState.IsGameOver || _gameState.IsLevelComplete) return;
            bool reserveEmpty = _superUndoBooster == null || !_superUndoBooster.HasReservedCar;
            if (_board.IsBoardEmpty() && reserveEmpty)
                _onLevelCompleteChannel.Raise();
        }

        private void HandleGameOver()
        {
            _gameState.IsGameOver = true;
            if (_debugLogging) Debug.Log("[GameManager] Game Over.");
        }

        private void HandleLevelComplete()
        {
            _gameState.IsLevelComplete = true;
            _currentLevelIndex++;
            SaveProgress();

            if (_debugLogging)
                Debug.Log($"[GameManager] Level Complete — index {_currentLevelIndex - 1} bitti, popup bekleniyor.");
            // Reset + Load, OnLevelContinueRequested gelince yapılır.
        }

        private void HandleLevelContinueRequested()
        {
            if (_debugLogging)
                Debug.Log($"[GameManager] Continue → level yükleniyor: index={_currentLevelIndex}");
            ResetLevelState();
            LoadCurrentLevel();
        }

        private void HandleRetryRequested()
        {
            if (_debugLogging)
                Debug.Log($"[GameManager] Retry → level yeniden yükleniyor: index={_currentLevelIndex}");
            ResetLevelState();
            LoadCurrentLevel();
        }

        private void BroadcastGameState()
        {
            foreach (var kvp in _gameState.BoosterCounts)
            {
                _onBoosterCountChangedChannel?.Raise(new BoosterCountPayload
                {
                    Type = kvp.Key,
                    Count = kvp.Value
                });
            }
            _onCoinsChangedChannel?.Raise(_gameState.Coins);
        }

        private void ResetLevelState()
        {
            _gameState.IsLevelComplete = false;
            _gameState.IsGameOver = false;
            _gameState.MovesUsedCount = 0;
            _holder?.ClearAllSlots();
            // BoosterCounts ve Coins level'lar arası korunur.
        }

        // ── Save / Load ─────────────────────────────────────────────────────

        private void LoadSaveData()
        {
            if (_saveManager == null)
            {
                Debug.LogWarning("[GameManager] SaveManager atanmamış — varsayılan değerler kullanılacak.");
                ApplyDefaultBoosterCounts();
                return;
            }

            var data = _saveManager.Load();
            _currentLevelIndex = data.currentLevelIndex;

            if (_levelTransitionData != null && _levelTransitionData.HasPendingSelection)
            {
                _currentLevelIndex = _levelTransitionData.SelectedLevelIndex;
                _levelTransitionData.HasPendingSelection = false;
            }

            _gameState.Coins = data.coins;

            if (data.boosters != null && data.boosters.Count > 0)
            {
                foreach (var entry in data.boosters)
                    _gameState.BoosterCounts[entry.type] = entry.count;

                // Kayıtta bulunmayan booster tipleri için default ata.
                for (int i = 0; i < _allBoosterTypes.Length; i++)
                {
                    if (!_gameState.BoosterCounts.ContainsKey(_allBoosterTypes[i]))
                        _gameState.BoosterCounts[_allBoosterTypes[i]] = _defaultBoosterCounts[i];
                }
            }
            else
            {
                ApplyDefaultBoosterCounts();
            }
        }

        private void ApplyDefaultBoosterCounts()
        {
            for (int i = 0; i < _allBoosterTypes.Length; i++)
                _gameState.BoosterCounts[_allBoosterTypes[i]] = _defaultBoosterCounts[i];
        }

        [ContextMenu("Reset Save Data")]
        private void ResetSaveData()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameManager] Reset Save Data sadece Play modunda çalışır.");
                return;
            }
            _saveManager?.Delete();
            _currentLevelIndex = 0;
            _gameState.Coins = 0;
            _gameState.IsGameOver = false;
            _gameState.IsLevelComplete = false;
            _gameState.MovesUsedCount = 0;
            ApplyDefaultBoosterCounts();
            LoadCurrentLevel();
            Debug.Log("[GameManager] Save data sıfırlandı — index=0, booster=3.");
        }

        private void SaveProgress()
        {
            if (_saveManager == null) return;
            _saveManager.Save(BuildSaveData());
        }

        private SaveData BuildSaveData()
        {
            var data = new SaveData
            {
                currentLevelIndex = _currentLevelIndex,
                coins = _gameState.Coins,
            };

            foreach (var kvp in _gameState.BoosterCounts)
                data.boosters.Add(new BoosterSaveEntry { type = kvp.Key, count = kvp.Value });

            return data;
        }
    }
}
