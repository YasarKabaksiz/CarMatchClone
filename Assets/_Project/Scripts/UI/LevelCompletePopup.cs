using CarMatchClone.Core.Events;
using TMPro;
using UnityEngine;

namespace CarMatchClone.UI
{
    public class LevelCompletePopup : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;
        [SerializeField] private VoidEventChannel _onLevelContinueRequestedChannel;

        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _coinRewardText;

        private void Awake()
        {
            _popupPanel.SetActive(false);
            _onLevelCompleteChannel.Subscribe(Show);
        }

        private void OnDestroy()
        {
            _onLevelCompleteChannel.Unsubscribe(Show);
        }

        private void Show()
        {
            // Coin kazanma mekaniği M10'da eklenecek.
            if (_coinRewardText != null)
                _coinRewardText.text = "+0";

            _popupPanel.SetActive(true);
        }

        // Continue butonunun onClick'ine bağlanır.
        public void OnContinueClicked()
        {
            _onLevelContinueRequestedChannel.Raise();
            _popupPanel.SetActive(false);
        }
    }
}
