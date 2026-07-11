using UnityEngine;
using CarMatchClone.Core;

namespace CarMatchClone.Boosters
{
    public class ShuffleBooster : MonoBehaviour, IBooster
    {
        public bool Execute(CarMatchClone.Board.Board board, GameState state)
        {
            board.ShuffleFruitTypes();
            return true;
        }
    }
}
