using UnityEngine;
using CarMatchClone.Data;

namespace CarMatchClone.Gameplay
{
    public class Car : MonoBehaviour
    {
        public Vector2Int GridPosition { get; set; }
        public bool IsReachable { get; set; }
        public CarColor Color { get; set; }
        public GameObject SourcePrefab { get; set; }
    }
}
