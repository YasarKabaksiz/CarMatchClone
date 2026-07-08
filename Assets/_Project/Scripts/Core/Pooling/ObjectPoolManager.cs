using System.Collections.Generic;
using UnityEngine;

namespace CarMatchClone.Core.Pooling
{
    // Tüm pool'ların merkezi kaydı. Prefab referansı → ObjectPool eşlemesi tutar.
    // Singleton değil: ihtiyaç duyan sistemler [SerializeField] ile referans alır.
    public class ObjectPoolManager : MonoBehaviour
    {
        private readonly Dictionary<GameObject, ObjectPool> _pools =
            new Dictionary<GameObject, ObjectPool>();

        // Pool yoksa oluşturur; varsa ek kapasite ekler.
        public void WarmUp(GameObject prefab, int capacity)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool(prefab, transform);
                _pools[prefab] = pool;
            }
            pool.WarmUp(capacity);
        }

        // Pool'dan obje alır. Pool kayıtlı değilse on-demand oluşturur.
        public GameObject Get(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool(prefab, transform);
                _pools[prefab] = pool;
            }
            return pool.Get();
        }

        // Objeyi ilgili pool'a iade eder.
        public void Release(GameObject prefab, GameObject instance)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                pool.Release(instance);
                return;
            }
            Debug.LogWarning($"[ObjectPoolManager] '{prefab.name}' için pool bulunamadı. Obje yok ediliyor.");
            Destroy(instance);
        }
    }
}
