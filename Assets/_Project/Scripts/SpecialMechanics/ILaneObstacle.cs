using UnityEngine;
using CarMatchClone.Board;
using CarMatchClone.Core.Events;

namespace CarMatchClone.SpecialMechanics
{
    public interface ILaneObstacle
    {
        // Board tarafından Instantiate sonrası çağrılır.
        void Initialize(Vector2Int gridPos, CarMatchClone.Board.Board board, CellEventChannel onCellVacatedChannel);
        bool IsActive { get; }
    }
}
