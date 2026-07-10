using CarMatchClone.Core.Events;
using UnityEngine;

namespace CarMatchClone.UI
{
    public class GameOverPopup : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGameOverChannel;
        [SerializeField] private VoidEventChannel _onRetryRequestedChannel;
        [SerializeField] private VoidEventChannel _onMainMenuRequestedChannel;

        [SerializeField] private GameObject _popupPanel;

        private void Awake()
        {
            _popupPanel.SetActive(false);
            _onGameOverChannel.Subscribe(Show);
        }

        private void OnDestroy()
        {
            _onGameOverChannel.Unsubscribe(Show);
        }

        private void Show()
        {
            _popupPanel.SetActive(true);
        }

        // Retry butonunun onClick'ine bağlanır.
        public void OnRetryClicked()
        {
            _popupPanel.SetActive(false);
            _onRetryRequestedChannel.Raise();
        }

        // Ana Menü butonunun onClick'ine bağlanır.
        public void OnMainMenuClicked()
        {
            _popupPanel.SetActive(false);
            _onMainMenuRequestedChannel.Raise();
        }
    }
}
