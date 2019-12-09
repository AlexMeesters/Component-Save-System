using System;
using System.Collections.Generic;
using UnityEngine;

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
            public string scene;
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

        // Used to track which save ids are assigned to a specific scene
        // This makes it posible to wipe all data from a specific scene.
        [NonSerialized] private Dictionary<string, List<string>> sceneObjectIDS = new Dictionary<string, List<string>>();

        public void OnWrite()
        {
            if (creationDate == default(DateTime))
            {
                creationDate = DateTime.Now;
            }

            metaData.creationDate = creationDate.ToString();
            metaData.gameVersion = gameVersion;
            metaData.timePlayed = timePlayed.ToString();
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
                    AddSceneID(saveData[i].scene, saveData[i].guid);
                }
            }
        }

        public void WipeSceneData(string sceneName)
        {
            List<string> value;
            if (sceneObjectIDS.TryGetValue(sceneName, out value))
            {
                int elementCount = value.Count;
                for (int i = elementCount - 1; i >= 0; i--)
                {
                    Remove(value[i]);
                    value.RemoveAt(i);
                }
            }
            else
            {
                Debug.Log("Scene is already wiped!");
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
                sceneObjectIDS.Remove(id);
            }
        }

        /// <summary>
        /// Assign any data to the given ID. If data is already present within the ID, then it will be overwritten.
        /// </summary>
        /// <param name="id"> Save Identification </param>
        /// <param name="data"> Data in a string format </param>
        public void Set(string id, string data, string scene)
        {
            int saveIndex;

            if (saveDataCache.TryGetValue(id, out saveIndex))
            {
                saveData[saveIndex] = new Data() { guid = id, data = data, scene = scene };
            }
            else
            {
                Data newSaveData = new Data() { guid = id, data = data, scene = scene };

                saveData.Add(newSaveData);
                saveDataCache.Add(id, saveData.Count - 1);
                AddSceneID(scene, id);
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

        /// <summary>
        /// Adds the index to a list that is identifyable by scene
        /// Makes it easy to remove save data related to a scene name.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="index"></param>
        private void AddSceneID(string scene, string id)
        {
            List<string> value;
            if (sceneObjectIDS.TryGetValue(scene, out value))
            {
                value.Add(id);
            }
            else
            {
                List<string> list = new List<string>();
                list.Add(id);
                sceneObjectIDS.Add(scene, list);
            }
        }
    }
}