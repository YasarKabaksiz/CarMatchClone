using CarMatchClone.Core.SaveSystem;
using CarMatchClone.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarMatchClone.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private LevelTransitionData _levelTransitionData;
        [SerializeField] private GameObject _levelButtonPrefab;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private int _totalLevelCount = 7;
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        private void Start()
        {
            Debug.Log("[MainMenuController] Start() çalıştı.");
            Debug.Log($"[MainMenuController] _saveManager      = {(_saveManager      == null ? "NULL" : _saveManager.name)}");
            Debug.Log($"[MainMenuController] _levelTransitionData = {(_levelTransitionData == null ? "NULL" : _levelTransitionData.name)}");
            Debug.Log($"[MainMenuController] _levelButtonPrefab = {(_levelButtonPrefab == null ? "NULL" : _levelButtonPrefab.name)}");
            Debug.Log($"[MainMenuController] _gridContainer    = {(_gridContainer    == null ? "NULL" : _gridContainer.name)}");
            Debug.Log($"[MainMenuController] _totalLevelCount  = {_totalLevelCount}");

            if (_saveManager == null)
            {
                Debug.LogError("[MainMenuController] HATA: SaveManager atanmamış — Inspector'da bağla.");
                return;
            }
            if (_levelButtonPrefab == null)
            {
                Debug.LogError("[MainMenuController] HATA: LevelButtonPrefab atanmamış — Inspector'da bağla.");
                return;
            }
            if (_gridContainer == null)
            {
                Debug.LogError("[MainMenuController] HATA: GridContainer atanmamış — Inspector'da ScrollView Content transform'unu bağla.");
                return;
            }

            var saveData = _saveManager.Load();
            int unlockedUpTo = saveData.currentLevelIndex;
            Debug.Log($"[MainMenuController] SaveData yüklendi — currentLevelIndex={saveData.currentLevelIndex}, unlockedUpTo={unlockedUpTo}");

            for (int i = 0; i < _totalLevelCount; i++)
            {
                var go = Instantiate(_levelButtonPrefab, _gridContainer);
                var btn = go.GetComponent<LevelButton>();
                if (btn == null)
                {
                    Debug.LogError($"[MainMenuController] HATA: Prefab üzerinde LevelButton bileşeni bulunamadı (index={i}).");
                    continue;
                }
                btn.Initialize(i, i <= unlockedUpTo, OnLevelSelected);
                Debug.Log($"[MainMenuController] Buton oluşturuldu: index={i}, unlocked={i <= unlockedUpTo}");
            }

            Debug.Log($"[MainMenuController] Toplam {_totalLevelCount} buton oluşturma tamamlandı.");
        }

        private void OnLevelSelected(int levelIndex)
        {
            _levelTransitionData.SelectedLevelIndex = levelIndex;
            _levelTransitionData.HasPendingSelection = true;
            SceneManager.LoadSceneAsync(_gameplaySceneName);
        }
    }
}
