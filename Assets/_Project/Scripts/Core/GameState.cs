using System.Collections.Generic;
using CarMatchClone.Data;

namespace CarMatchClone.Core
{
    public class GameState
    {
        public bool IsGameOver { get; set; }
        public bool IsLevelComplete { get; set; }
        public int MovesUsedCount { get; set; }
        public Dictionary<BoosterType, int> BoosterCounts { get; } = new Dictionary<BoosterType, int>();
    }
}
