using UnityEngine;

namespace CarMatchClone.Gameplay
{
    public class Car : MonoBehaviour
    {
        public Vector2Int GridPosition { get; set; }
        public bool IsReachable { get; set; }
    }
}
