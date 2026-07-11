using DG.Tweening;
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

        [Header("Spawn Efekti")]
        [SerializeField] private float _spawnPopDuration  = 0.25f;  // scale 0→1 süresi
        [SerializeField] private float _spawnHopHeight    = 0.12f;  // yukarı sıçrama mesafesi (local Y)
        [SerializeField] private float _spawnHopDuration  = 0.12f;  // yukarı / aşağı her adım süresi

        // GarageSpawner spawn olduğunda Board tarafından çağrılır.
        // Mantık (OnBoardStateChanged) zaten tetiklendikten SONRA çağrıldığından
        // oyun akışını geciktirmez.
        public void PlaySpawnEffect()
        {
            // Prefab'ın yazarlanmış scale'ini hedef al; runtime localScale kirlenmiş olabilir
            // (önceki DOScale(Vector3.one) çağrısı yanlış değer bırakmış olabilir).
            Vector3 targetScale = SourcePrefab != null
                ? SourcePrefab.transform.localScale
                : transform.localScale;

            transform.localScale = Vector3.zero;
            float startY   = transform.localPosition.y;
            float hopStart = _spawnPopDuration * 0.4f;  // pop'un %40'ında sıçrama başlar

            DOTween.Sequence()
                .Append(transform.DOScale(targetScale, _spawnPopDuration)
                    .SetEase(Ease.OutBack))
                .Insert(hopStart,
                    transform.DOLocalMoveY(startY + _spawnHopHeight, _spawnHopDuration)
                        .SetEase(Ease.OutQuad))
                .Insert(hopStart + _spawnHopDuration,
                    transform.DOLocalMoveY(startY, _spawnHopDuration)
                        .SetEase(Ease.InQuad));
        }
    }
}
