using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarMatchClone.UI
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _levelNumberText;
        [SerializeField] private GameObject _lockOverlay;

        public void Initialize(int levelIndex, bool isUnlocked, System.Action<int> onSelected)
        {
            _levelNumberText.text = (levelIndex + 1).ToString();
            _lockOverlay.SetActive(!isUnlocked);
            _button.interactable = isUnlocked;

            _button.onClick.RemoveAllListeners();
            if (isUnlocked)
                _button.onClick.AddListener(() => onSelected(levelIndex));
        }
    }
}
