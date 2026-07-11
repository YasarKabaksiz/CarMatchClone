using UnityEngine;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core.Events
{
    [CreateAssetMenu(menuName = "CarMatchClone/Events/FruitEventChannel")]
    public class FruitEventChannel : GameEventChannel<Fruit> { }
}
