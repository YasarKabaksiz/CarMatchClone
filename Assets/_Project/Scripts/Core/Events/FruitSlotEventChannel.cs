using CarMatchClone.Gameplay;
using UnityEngine;

namespace CarMatchClone.Core.Events
{
    [CreateAssetMenu(menuName = "CarMatchClone/Events/FruitSlotEventChannel")]
    public class FruitSlotEventChannel : GameEventChannel<SlotAssignedPayload> { }
}
