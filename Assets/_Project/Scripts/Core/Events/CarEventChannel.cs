using UnityEngine;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core.Events
{
    [CreateAssetMenu(menuName = "CarMatchClone/Events/CarEventChannel")]
    public class CarEventChannel : GameEventChannel<Car> { }
}
