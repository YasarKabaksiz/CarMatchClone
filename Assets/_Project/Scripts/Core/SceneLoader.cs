using UnityEngine;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Core
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onMainMenuRequestedChannel;

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
            // MainMenu sahnesi M9'un sonraki adımında kurulacak — şimdilik no-op.
            // TODO M9: SceneManager.LoadSceneAsync(_mainMenuSceneName)
            Debug.Log("[SceneLoader] OnMainMenuRequested alındı — MainMenu sahnesi henüz kurulmadı.");
        }
    }
}
