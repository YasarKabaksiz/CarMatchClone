using UnityEngine;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CarMatchClone.Board.Board _board;
        [SerializeField] private Holder _holder;
        [SerializeField] private Camera _camera;

        // Yataydan ölçülen açı: 90° = tam dikey, 65° = hafif öne eğik
        [SerializeField][Range(30f, 90f)] private float _tiltAngle = 65f;
        [SerializeField] private float _padding = 1.5f;

        private void Start()
        {
            if (_camera == null)
                _camera = Camera.main;
            FrameBoard();
        }

        // Level geçişlerinde dışarıdan tekrar çağrılabilir.
        [ContextMenu("Frame Board")]
        public void FrameBoard()
        {
            Bounds bounds = _board.GetWorldBounds();
            if (_holder != null)
                bounds.Encapsulate(_holder.GetBounds());

            float tiltRad    = _tiltAngle * Mathf.Deg2Rad;
            float halfFovRad = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float tanHalfFov = Mathf.Tan(halfFovRad);

            // Toplam alanın Z boyutu, kameranın dikey eksenine sin(tilt) oranında yansır.
            float verticalFit   = (bounds.extents.z * Mathf.Sin(tiltRad) + _padding) / tanHalfFov;
            float horizontalFit = (bounds.extents.x + _padding) / (tanHalfFov * _camera.aspect);
            float D = Mathf.Max(verticalFit, horizontalFit);

            Vector3 center = bounds.center;
            _camera.transform.SetPositionAndRotation(
                new Vector3(
                    center.x,
                    center.y + D * Mathf.Sin(tiltRad),
                    center.z - D * Mathf.Cos(tiltRad)),
                Quaternion.Euler(_tiltAngle, 0f, 0f));
        }
    }
}
