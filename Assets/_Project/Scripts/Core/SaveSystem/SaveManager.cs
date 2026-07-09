using UnityEngine;

namespace CarMatchClone.Core.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private bool _debugLogging;

        private ISaveProvider _provider;
        private ISaveProvider Provider => _provider ??= new LocalJsonSaveProvider();

        public string SaveFilePath => (Provider as LocalJsonSaveProvider)?.FilePath ?? "(bilinmiyor)";

        private void Awake()
        {
            _ = Provider; // Awake'te hazırla; lazy init zaten garantiler.
            if (_debugLogging)
                Debug.Log($"[SaveManager] Kayıt dosyası: {SaveFilePath}");
        }

        public void Save(SaveData data)
        {
            Provider.Save(data);
            if (_debugLogging)
                Debug.Log($"[SaveManager] Kaydedildi → level={data.currentLevelIndex}, coins={data.coins}");
        }

        public SaveData Load()
        {
            var data = Provider.Load();
            if (_debugLogging)
                Debug.Log($"[SaveManager] Yüklendi → level={data.currentLevelIndex}, hasSave={Provider.HasSave()}");
            return data;
        }

        public bool HasSave() => Provider.HasSave();

        [ContextMenu("Kayıt Dosyasını Sil")]
        public void Delete()
        {
            Provider.Delete();
            if (_debugLogging)
                Debug.Log("[SaveManager] Kayıt dosyası silindi.");
        }
    }
}
