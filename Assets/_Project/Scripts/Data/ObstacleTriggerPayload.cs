using UnityEngine;

namespace CarMatchClone.Data
{
    public struct ObstacleTriggerPayload
    {
        public Vector2Int Position;
        public System.Action UndoAction;
    }
}
