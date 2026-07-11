using UnityEngine;
using UnityEngine.SceneManagement;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Core
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onMainMenuRequestedChannel;
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        private void OnEnable()
        {
            _onMainMenuRequestedChannel?.Subscribe(HandleMainMenuRequested);
        }

        private void OnDisable()
        {
            _onMainMenuRequestedChannel?.Unsubscribe(HandleMainMenuRequested);
        }

        private void HandleMainMenuRequested()
        {
            SceneManager.LoadSceneAsync(_mainMenuSceneName);
        }
    }
}
