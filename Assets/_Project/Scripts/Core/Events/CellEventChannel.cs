using UnityEngine;

namespace CarMatchClone.Core.Events
{
    [CreateAssetMenu(menuName = "CarMatchClone/Events/CellEventChannel")]
    public class CellEventChannel : GameEventChannel<CarMatchClone.Board.GridCell>
    {
    }
}
