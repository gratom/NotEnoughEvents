namespace NEE
{
    using UnityEngine;

    public static class ResStorage
    {
        private const string Key = "NEEMod_Resources";

        public static void Save(ResSaveData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public static ResSaveData Load()
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                return null;
            }

            string json = PlayerPrefs.GetString(Key);
            return JsonUtility.FromJson<ResSaveData>(json);
        }
    }

    [System.Serializable]
    public class ResSaveData
    {
        public int gold;
        public int steel;
        public int wood;
        public int fabric;
    }

}