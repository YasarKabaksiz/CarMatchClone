using System.IO;
using UnityEngine;

namespace CarMatchClone.Core.SaveSystem
{
    public class LocalJsonSaveProvider : ISaveProvider
    {
        private readonly string _filePath;

        public LocalJsonSaveProvider(string fileName = "save.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public string FilePath => _filePath;

        public void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(_filePath, json);
        }

        public SaveData Load()
        {
            if (!HasSave()) return new SaveData();
            string json = File.ReadAllText(_filePath);
            return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }

        public bool HasSave() => File.Exists(_filePath);

        public void Delete()
        {
            if (HasSave()) File.Delete(_filePath);
        }
    }
}
