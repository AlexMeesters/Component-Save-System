using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lowscope.Saving.Data
{
    /// <summary>
    /// Container for all saved data.
    /// Placed into a slot (separate save file)
    /// </summary>
    [Serializable]
    public class SaveGame
    {
        [Serializable]
        public struct MetaData
        {
            public int gameVersion;
            public string creationDate;
            public string timePlayed;
        }

        [Serializable]
        public struct Data
        {
            public string guid;
            public string data;
        }

        [NonSerialized] public TimeSpan timePlayed;
        [NonSerialized] public int gameVersion;
        [NonSerialized] public DateTime creationDate;

        [SerializeField] private MetaData metaData;
        [SerializeField] private List<Data> saveData = new List<Data>();

        // Stored in dictionary for quick lookup
        [NonSerialized]
        private Dictionary<string, int> saveDataCache = new Dictionary<string, int>(StringComparer.Ordinal);

        [NonSerialized] private bool loaded;

        public void OnWrite()
        {
            if (creationDate == default(DateTime))
            {
                creationDate = DateTime.Now;
            }

            metaData.creationDate = creationDate.ToString();
            metaData.gameVersion = gameVersion;
            metaData.timePlayed = timePlayed.ToString();

            int sceneCount = SceneManager.sceneCount;
        }

        public void OnLoad()
        {
            gameVersion = metaData.gameVersion;

            DateTime.TryParse(metaData.creationDate, out creationDate);
            TimeSpan.TryParse(metaData.timePlayed, out timePlayed);

            if (saveData.Count > 0)
            {
                // Clear all empty data on load.
                int dataCount = saveData.Count;
                for (int i = dataCount - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(saveData[i].data))
                        saveData.RemoveAt(i);
                }

                for (int i = 0; i < saveData.Count; i++)
                {
                    saveDataCache.Add(saveData[i].guid, i);
                }
            }
        }

        public void Remove(string id)
        {
            int saveIndex;

            if (saveDataCache.TryGetValue(id, out saveIndex))
            {
                // Zero out the string data, it will be wiped on next cache initialization.
                saveData[saveIndex] = new Data();
                saveDataCache.Remove(id);
            }
        }

        /// <summary>
        /// Assign any data to the given ID. If data is already present within the ID, then it will be overwritten.
        /// </summary>
        /// <param name="id"> Save Identification </param>
        /// <param name="data"> Data in a string format </param>
        public void Set(string id, string data)
        {
            int saveIndex;

            if (saveDataCache.TryGetValue(id, out saveIndex))
            {
                saveData[saveIndex] = new Data() {guid = id, data = data};
            }
            else
            {
                Data newSaveData = new Data() {guid = id, data = data};

                saveData.Add(newSaveData);
                saveDataCache.Add(id, saveData.Count - 1);
            }
        }

        /// <summary>
        /// Returns any data stored based on a identifier
        /// </summary>
        /// <param name="id"> Save Identification </param>
        /// <returns></returns>
        public string Get(string id)
        {
            int saveIndex;

            if (saveDataCache.TryGetValue(id, out saveIndex))
            {
                return saveData[saveIndex].data;
            }
            else
            {
                return null;
            }
        }
    }
}