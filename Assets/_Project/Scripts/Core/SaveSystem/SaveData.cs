using System;
using System.Collections.Generic;
using CarMatchClone.Data;

namespace CarMatchClone.Core.SaveSystem
{
    [Serializable]
    public class SaveData
    {
        public int currentLevelIndex = 0;
        public int coins = 0;
        public List<BoosterSaveEntry> boosters = new List<BoosterSaveEntry>();
    }

    [Serializable]
    public class BoosterSaveEntry
    {
        public BoosterType type;
        public int count;
    }
}
