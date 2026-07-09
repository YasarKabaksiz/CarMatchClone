using CarMatchClone.Data;
using UnityEngine;

namespace CarMatchClone.Core.Events
{
    [CreateAssetMenu(menuName = "CarMatchClone/Events/ObstacleEventChannel")]
    public class ObstacleEventChannel : GameEventChannel<ObstacleTriggerPayload> { }
}
