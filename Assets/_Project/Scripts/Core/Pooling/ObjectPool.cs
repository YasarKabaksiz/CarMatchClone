using System.Collections.Generic;
using UnityEngine;

namespace CarMatchClone.Core.Pooling
{
    // Tek bir prefab tipine ait pool. ObjectPoolManager tarafından yönetilir.
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly Queue<GameObject> _queue = new Queue<GameObject>();

        public ObjectPool(GameObject prefab, Transform container)
        {
            _prefab = prefab;
            _container = container;
        }

        // Belirtilen sayıda objeyi önceden oluşturur ve kuyruğa alır.
        public void WarmUp(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = Object.Instantiate(_prefab, _container);
                obj.SetActive(false);
                _queue.Enqueue(obj);
            }
            Debug.Log($"[Pool:{_prefab.name}] WarmUp tamamlandı — {count} obje önceden oluşturuldu, kuyruk: {_queue.Count}");
        }

        // Kuyrukta bekleyen varsa döndürür; yoksa yeni Instantiate eder.
        // Caller: pozisyon, parent ve SetActive(true) kendi sorumluluğunda.
        public GameObject Get()
        {
            if (_queue.Count > 0)
            {
                var obj = _queue.Dequeue();
                Debug.Log($"[Pool:{_prefab.name}] GET — pool'dan alındı. Kalan kuyruk: {_queue.Count}");
                return obj;
            }

            Debug.LogWarning($"[Pool:{_prefab.name}] GET — kuyruk boş, yeni Instantiate yapıldı!");
            return Object.Instantiate(_prefab, _container);
        }

        // Objeyi deaktive edip pool container'ına iade eder.
        public void Release(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(_container);
            _queue.Enqueue(obj);
            Debug.Log($"[Pool:{_prefab.name}] RELEASE — kuyruk: {_queue.Count}");
        }
    }
}
