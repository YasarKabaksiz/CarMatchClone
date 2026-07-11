using CarMatchClone.Core.Events;
using TMPro;
using UnityEngine;

namespace CarMatchClone.UI
{
    public class LevelCompletePopup : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onLevelCompleteChannel;
        [SerializeField] private VoidEventChannel _onLevelContinueRequestedChannel;
        [SerializeField] private IntEventChannel _onCoinRewardEarnedChannel;

        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _coinRewardText;

        private void Awake()
        {
            _popupPanel.SetActive(false);
        }

        private void OnEnable()
        {
            _onLevelCompleteChannel.Subscribe(Show);
            _onCoinRewardEarnedChannel?.Subscribe(HandleCoinReward);
        }

        private void OnDisable()
        {
            _onLevelCompleteChannel.Unsubscribe(Show);
            _onCoinRewardEarnedChannel?.Unsubscribe(HandleCoinReward);
        }

        private void Show()
        {
            _popupPanel.SetActive(true);
        }

        private void HandleCoinReward(int amount)
        {
            if (_coinRewardText != null)
                _coinRewardText.text = $"+{amount}";
        }

        // Continue butonunun onClick'ine bağlanır.
        public void OnContinueClicked()
        {
            _onLevelContinueRequestedChannel.Raise();
            _popupPanel.SetActive(false);
        }
    }
}
