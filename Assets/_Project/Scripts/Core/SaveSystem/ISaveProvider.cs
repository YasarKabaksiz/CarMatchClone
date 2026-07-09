namespace CarMatchClone.Core.SaveSystem
{
    public interface ISaveProvider
    {
        void Save(SaveData data);
        SaveData Load();
        bool HasSave();
        void Delete();
    }
}
