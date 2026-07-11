using DG.Tweening;
using UnityEngine;
using CarMatchClone.Board;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;

namespace CarMatchClone.SpecialMechanics
{
    public class GarageSpawner : MonoBehaviour, ILaneObstacle
    {
        [SerializeField] private Transform _previewAnchor;          // bowl üzerindeki boş child (Inspector'dan atanacak)
        [SerializeField] private float _previewScale   = 0.65f;     // bekleyen meyve görsel küçültme faktörü
        [SerializeField] private float _jumpDuration   = 0.35f;
        [SerializeField] private float _jumpArcHeight  = 1.2f;
        [SerializeField] private float _revealDuration = 0.12f;

        private Vector2Int _gridPos;
        private Vector2Int _facingCell;
        private CarMatchClone.Board.Board _board;
        private CellEventChannel _onCellVacatedChannel;
        private ObstacleEventChannel _onObstacleTriggeredChannel;
        private FruitType[] _fruitTypes;
        private int _currentSpawnIndex;
        private GameObject _previewVisual;

        public bool IsActive => _fruitTypes != null && _currentSpawnIndex < _fruitTypes.Length;

        public void Initialize(Vector2Int gridPos, CarMatchClone.Board.Board board, CellEventChannel onCellVacatedChannel)
        {
            _gridPos = gridPos;
            _board = board;
            _onCellVacatedChannel = onCellVacatedChannel;
            _onCellVacatedChannel.Subscribe(OnCellVacated);
        }

        public void Setup(FruitType[] fruitTypes, FacingDirection facing, ObstacleEventChannel onObstacleTriggeredChannel)
        {
            _fruitTypes = fruitTypes;
            _currentSpawnIndex = 0;
            _facingCell = _gridPos + facing.ToVector();
            _onObstacleTriggeredChannel = onObstacleTriggeredChannel;
            RefreshPreview();
        }

        private void OnDisable()
        {
            _onCellVacatedChannel?.Unsubscribe(OnCellVacated);
            DestroyPreview();
        }

        private void OnCellVacated(GridCell cell)
        {
            if (!IsActive) return;
            if (cell.Position != _facingCell) return;
            Trigger();
        }

        private void Trigger()
        {
            var fruitType = _fruitTypes[_currentSpawnIndex];
            _currentSpawnIndex++;

            // Oyun mantığı: hemen çalışır — pathfinding ve grid state güncel kalır.
            _board.SpawnFromGarage(_facingCell, fruitType);

            // Gerçek meyveyi jump tamamlanana kadar gizle.
            var placedTr = _board.GetCell(_facingCell)?.Occupant?.transform;
            if (placedTr != null)
                placedTr.localScale = Vector3.zero;

            AnimateJump(_board.GridToWorld(_facingCell), placedTr, fruitType);

            _onObstacleTriggeredChannel?.Raise(new ObstacleTriggerPayload
            {
                Position  = _facingCell,
                UndoAction = UndoLastSpawn
            });
        }

        private void AnimateJump(Vector3 target, Transform placedTr, FruitType fruitType)
        {
            if (_previewVisual == null) return;

            var preview = _previewVisual;
            _previewVisual = null;   // ownership animasyona devredildi

            float halfDur = _jumpDuration * 0.5f;
            float startY  = preview.transform.position.y;

            var seq = DOTween.Sequence();
            // Y: parabolik ark (yukarı → hedef)
            seq.Append(preview.transform.DOMoveY(startY + _jumpArcHeight, halfDur).SetEase(Ease.OutQuad));
            seq.Append(preview.transform.DOMoveY(target.y,                halfDur).SetEase(Ease.InQuad));
            // XZ: lineer — Y tweenleriyle çakışmaz (farklı eksenler)
            seq.Insert(0f, preview.transform.DOMoveX(target.x, _jumpDuration).SetEase(Ease.Linear));
            seq.Insert(0f, preview.transform.DOMoveZ(target.z, _jumpDuration).SetEase(Ease.Linear));

            seq.OnComplete(() =>
            {
                Destroy(preview);

                // Gerçek meyveyi mini-pop ile göster.
                if (placedTr != null && placedTr.gameObject.activeInHierarchy)
                {
                    var srcPrefab  = _board.GetFruitPrefab(fruitType);
                    var finalScale = srcPrefab != null ? srcPrefab.transform.localScale : Vector3.one;
                    placedTr.DOScale(finalScale, _revealDuration).SetEase(Ease.OutBack);
                }

                if (IsActive) RefreshPreview();
            });
        }

        // --- Preview oluşturma ---

        private void RefreshPreview()
        {
            DestroyPreview();
            if (!IsActive) return;
            if (_previewAnchor == null)
            {
                Debug.LogWarning("[GarageSpawner] _previewAnchor atanmamış — preview devre dışı.", this);
                return;
            }

            var fruitType = _fruitTypes[_currentSpawnIndex];
            var srcPrefab = _board.GetFruitPrefab(fruitType);
            if (srcPrefab == null)
            {
                Debug.LogWarning($"[GarageSpawner] {fruitType} için prefab bulunamadı — preview atlandı.", this);
                return;
            }

            _previewVisual = BuildPreviewVisual(srcPrefab, fruitType);
            _previewVisual.transform.SetPositionAndRotation(_previewAnchor.position, Quaternion.identity);
        }

        private GameObject BuildPreviewVisual(GameObject srcPrefab, FruitType fruitType)
        {
            var root = new GameObject($"GaragePreview_{fruitType}");
            root.transform.SetParent(transform);
            // Prefab'ın yazarlanmış scale'i × küçültme faktörü
            root.transform.localScale = srcPrefab.transform.localScale * _previewScale;

            // Root'ta mesh varsa kopyala (Tomato, Lemon, Orange, Coconut, Grape...)
            CopyMeshIfPresent(srcPrefab, root);

            // Child'larda mesh varsa kopyala (Watermelon: 8 slice child)
            foreach (Transform child in srcPrefab.transform)
            {
                if (child.GetComponent<MeshFilter>() == null) continue;
                var childCopy = new GameObject(child.name);
                childCopy.transform.SetParent(root.transform);
                childCopy.transform.localPosition = child.localPosition;
                childCopy.transform.localRotation = child.localRotation;
                childCopy.transform.localScale    = child.localScale;
                CopyMeshIfPresent(child.gameObject, childCopy);
            }

            return root;
        }

        private static void CopyMeshIfPresent(GameObject src, GameObject dst)
        {
            var mf = src.GetComponent<MeshFilter>();
            var mr = src.GetComponent<MeshRenderer>();
            if (mf == null || mr == null) return;
            dst.AddComponent<MeshFilter>().sharedMesh        = mf.sharedMesh;
            dst.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
        }

        private void DestroyPreview()
        {
            if (_previewVisual == null) return;
            Destroy(_previewVisual);
            _previewVisual = null;
        }

        private void UndoLastSpawn()
        {
            _currentSpawnIndex--;
            _board.RemoveFruitAt(_facingCell);
            RefreshPreview();   // undo sonrası preview geri dön
        }
    }
}
