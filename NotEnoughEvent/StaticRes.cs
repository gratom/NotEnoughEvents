namespace NEE
{
    public class StaticRes
    {
        public static ResSaveData Data { get; private set; }

        static StaticRes()
        {
            Load();
        }

        public static void Load()
        {
            Data = ResStorage.Load() ?? new ResSaveData();
        }

        public static void Save()
        {
            ResStorage.Save(Data);
        }

    }
}