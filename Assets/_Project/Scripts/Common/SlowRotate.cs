using UnityEngine;

namespace CarMatchClone.Common
{
    public class SlowRotate : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 45f;

        private void Update()
        {
            transform.Rotate(Vector3.up, _degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
