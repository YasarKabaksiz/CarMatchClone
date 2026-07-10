using System.Collections.Generic;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;
using TMPro;
using UnityEngine;

namespace CarMatchClone.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private BoosterCountEventChannel _onBoosterCountChangedChannel;
        [SerializeField] private BoosterEventChannel _onBoosterUsedChannel;
        [SerializeField] private IntEventChannel _onCoinsChangedChannel;

        [SerializeField] private BoosterButton[] _boosterButtons;
        [SerializeField] private TMP_Text _coinText;

        private Dictionary<BoosterType, BoosterButton> _buttonMap;

        private void Awake()
        {
            _buttonMap = new Dictionary<BoosterType, BoosterButton>();
            foreach (var btn in _boosterButtons)
            {
                if (btn != null)
                    _buttonMap[btn.BoosterType] = btn;
            }
        }

        private void OnEnable()
        {
            _onBoosterCountChangedChannel.Subscribe(HandleBoosterCountChanged);
            _onBoosterUsedChannel.Subscribe(HandleBoosterUsed);
            _onCoinsChangedChannel.Subscribe(HandleCoinsChanged);
        }

        private void OnDisable()
        {
            _onBoosterCountChangedChannel.Unsubscribe(HandleBoosterCountChanged);
            _onBoosterUsedChannel.Unsubscribe(HandleBoosterUsed);
            _onCoinsChangedChannel.Unsubscribe(HandleCoinsChanged);
        }

        private void HandleBoosterCountChanged(BoosterCountPayload payload)
        {
            if (_buttonMap.TryGetValue(payload.Type, out var btn))
                btn.SetCount(payload.Count);
        }

        // Bağlantı noktası: M11'de buton press animasyonu/ses efekti buraya eklenir.
        private void HandleBoosterUsed(BoosterType type) { }

        private void HandleCoinsChanged(int coins)
        {
            if (_coinText != null)
                _coinText.text = coins.ToString();
        }
    }
}
