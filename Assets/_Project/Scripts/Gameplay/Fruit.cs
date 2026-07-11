using UnityEngine;
using CarMatchClone.Data;

namespace CarMatchClone.Gameplay
{
    public class Fruit : MonoBehaviour
    {
        public Vector2Int GridPosition { get; set; }
        public bool IsReachable { get; set; }
        public FruitType Color { get; set; }
        public GameObject SourcePrefab { get; set; }
    }
}
