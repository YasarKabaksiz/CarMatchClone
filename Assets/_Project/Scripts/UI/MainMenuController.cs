using CarMatchClone.Core.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarMatchClone.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private TMP_Text _levelIndicatorText;
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        private void Start()
        {
            var saveData = _saveManager != null ? _saveManager.Load() : new SaveData();

            if (_levelIndicatorText != null)
                _levelIndicatorText.text = $"Level {saveData.currentLevelIndex + 1}";

            if (_coinText != null)
                _coinText.text = saveData.coins.ToString();
        }

        // PlayButton'un onClick'ine bağlanır.
        public void OnPlayClicked()
        {
            SceneManager.LoadScene(_gameplaySceneName);
        }
    }
}
