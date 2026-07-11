using UnityEngine;

namespace CarMatchClone.Data
{
    [CreateAssetMenu(menuName = "CarMatchClone/LevelTransitionData")]
    public class LevelTransitionData : ScriptableObject
    {
        public int SelectedLevelIndex;
        public bool HasPendingSelection;
    }
}
