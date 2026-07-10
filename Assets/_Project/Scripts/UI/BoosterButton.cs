using CarMatchClone.Core.Events;
using CarMatchClone.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarMatchClone.UI
{
    public class BoosterButton : MonoBehaviour
    {
        [SerializeField] private BoosterType _boosterType;
        [SerializeField] private BoosterEventChannel _onBoosterRequestedChannel;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _countText;

        public BoosterType BoosterType => _boosterType;

        private void Start()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        public void SetCount(int count)
        {
            _countText.text = count.ToString();
            _button.interactable = count > 0;
        }

        private void OnClicked()
        {
            _onBoosterRequestedChannel?.Raise(_boosterType);
        }
    }
}
