using System.Collections.Generic;
using System.Linq;
using Lowscope.Saving.Data;
using Lowscope.Saving;
using UnityEditor;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    /// <summary>
    /// Attach this to the root of an object that you want to save
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-9001)]
    [AddComponentMenu("Saving/Saveable")]
    public class Saveable : MonoBehaviour
    {
        [Header("Save configuration")]
        [SerializeField, Tooltip("Will never allow the object to load data more then once." +
                                 "this is useful for persistent game objects.")]
        private bool loadOnce;

        [SerializeField, Tooltip("Save and Load will not be called by the Save System." +
                                 "this is useful for displaying data from a different save file")]
        private bool manualSaveLoad;

        [SerializeField, Tooltip("It will scan other objects for ISaveable components")]
        private List<GameObject> externalListeners = new List<GameObject>();

        [SerializeField, HideInInspector]
        private List<SaveableComponent> saveableComponents = new List<SaveableComponent>();

        private Dictionary<string, ISaveable> saveableComponentDictionary = new Dictionary<string, ISaveable>();

        public string saveIdentification;

        private bool hasLoaded;

        /// <summary>
        /// Means of storing all saveable components for the ISaveable component.
        /// </summary>
        [System.Serializable]
        public class SaveableComponent
        {
            public string identifier;
            public MonoBehaviour monoBehaviour;
        }

        // Used to know if specific data is already contained within the save structure array
        private Dictionary<string, int> iSaveableDataIdentifiers = new Dictionary<string, int>();

        private ISaveableDataCollection iSaveableData = new ISaveableDataCollection();

        private SaveGame cachedSaveGame;

        public bool ManualSaveLoad
        {
            get { return manualSaveLoad; }
            set { manualSaveLoad = value; }
        }

#if UNITY_EDITOR

        private string identification;
        private static string identificationData;
        private static string lastSelectedGUID;

        private void SetIdentifcation(int index, string identifier)
        {
            saveableComponents[index].identifier = identifier;
        }

        private void Reset()
        {
            bool isPrefab;

#if UNITY_2018_3_OR_NEWER 
        isPrefab = PrefabUtility.IsPartOfPrefabAsset(this.gameObject);
#else
            isPrefab = this.gameObject.scene.name == null;
#endif

            // Set a new save identification if it is not a prefab at the moment.
            if (!isPrefab)
            {
                if (string.IsNullOrEmpty(saveIdentification))
                {
#if NET_4_6
                saveIdentification = $"{gameObject.scene.name}-{gameObject.name}-{System.Guid.NewGuid().ToString().Substring(0, 5)}";
#else
                    saveIdentification = string.Format("{0}-{1}-{2}", gameObject.scene.name, gameObject.name, System.Guid.NewGuid().ToString().Substring(0, 5));
#endif
                }
            }
            else
            {
                saveIdentification = string.Empty;
                EditorUtility.SetDirty(this.gameObject);
            }

            List<ISaveable> obtainSaveables = new List<ISaveable>();

            obtainSaveables.AddRange(GetComponentsInChildren<ISaveable>(true).ToList());
            for (int i = 0; i < externalListeners.Count; i++)
            {
                if (externalListeners[i] != null)
                    obtainSaveables.AddRange(externalListeners[i].GetComponentsInChildren<ISaveable>(true).ToList());
            }

            if (obtainSaveables.Count != saveableComponents.Count)
            {
                if (saveableComponents.Count > obtainSaveables.Count)
                {
                    for (int i = saveableComponents.Count - 1; i >= obtainSaveables.Count; i--)
                    {
                        saveableComponents.RemoveAt(i);
                    }
                }

                saveableComponents.RemoveAll(s => s.monoBehaviour == null);

                ISaveable[] cachedSaveables = new ISaveable[saveableComponents.Count];
                for (int i = 0; i < cachedSaveables.Length; i++)
                {
                    cachedSaveables[i] = saveableComponents[i].monoBehaviour as ISaveable;
                }

                ISaveable[] missingElements = obtainSaveables.Except(cachedSaveables).ToArray();

                for (int i = 0; i < missingElements.Length; i++)
                {
                    SaveableComponent newSaveableComponent = new SaveableComponent()
                    {
                        monoBehaviour = missingElements[i] as MonoBehaviour
                    };

                    string typeString = newSaveableComponent.monoBehaviour.GetType().ToString();
                    string guidString = System.Guid.NewGuid().ToString().Substring(0, 5);

                    newSaveableComponent.identifier = string.Format("{0} {1}", typeString, guidString);

                    saveableComponents.Add(newSaveableComponent);
                }
            }
        }

        public void Refresh()
        {
            Reset();
        }

#endif

        /// <summary>
        /// Gets and adds a saveable components. This is only required when you want to
        /// create gameobjects dynamically through C#. Keep in mind that changing the component add order
        /// will change the way it gets loaded.
        /// </summary>
        public void ScanAddSaveableComponents()
        {
            ISaveable[] saveables = GetComponentsInChildren<ISaveable>();

            for (int i = 0; i < saveables.Length; i++)
            {
                string mono = (saveables[i] as MonoBehaviour).name;

                AddSaveableComponent(string.Format("Dyn-{0}-{1}", mono, i.ToString()), saveables[i]);
            }
        }

        public void AddSaveableComponent(string identifier, ISaveable iSaveable)
        {
            saveableComponentDictionary.Add(identifier, iSaveable);

            // Load it again, to ensure all ISaveable interfaces are updated.
            if (cachedSaveGame != null)
            {
                OnLoadRequest(cachedSaveGame);
            }
        }

        private void Awake()
        {
            // Store the component identifiers into a dictionary for performant retrieval.
            for (int i = 0; i < saveableComponents.Count; i++)
            {
                saveableComponentDictionary.Add(saveableComponents[i].identifier, saveableComponents[i].monoBehaviour as ISaveable);
            }

            if (!manualSaveLoad)
            {
                SaveMaster.StartUpdating(this);
            }
        }

        private void OnDestroy()
        {
            if (!manualSaveLoad)
            {
                SaveMaster.StopUpdating(this);
            }
        }

        // Request is sent by the Save System
        public void OnSaveRequest(SaveGame saveGame)
        {
            if (cachedSaveGame != saveGame)
            {
                cachedSaveGame = saveGame;
            }

            if (string.IsNullOrEmpty(saveIdentification))
            {
                return;
            }

            bool hasSaved = false;

            // Get all saveable components
            foreach (KeyValuePair<string, ISaveable> item in saveableComponentDictionary)
            {
                int getSavedIndex = -1;

                // Skip if the component does not want to be saved
                if (item.Value.OnSaveCondition() == false)
                {
                    continue;
                }
                else
                {
                    hasSaved = true;
                }

                // Store the info from the ISaveable into a struct
                ISaveableData saveStructure = new ISaveableData()
                {
                    identifier = item.Key,
                    data = item.Value.OnSave()
                };

                // Store the structure into a collection, as there are multiple components that may have the ISaveable interface.
                if (!iSaveableDataIdentifiers.TryGetValue(item.Key, out getSavedIndex))
                {
                    iSaveableData.saveStructures.Add(saveStructure);
                    iSaveableDataIdentifiers.Add(item.Key, iSaveableData.saveStructures.Count - 1);
                }
                else
                {
                    iSaveableData.saveStructures[getSavedIndex] = saveStructure;
                }
            }

            if (hasSaved)
            {
                // The collection will be passed into the savegame using the identification that has been assigned to this component.
                saveGame.Set(saveIdentification, JsonUtility.ToJson(iSaveableData));
            }
        }

        // Request is sent by the Save System
        public void OnLoadRequest(SaveGame saveGame)
        {
            if (cachedSaveGame != saveGame)
            {
                if (cachedSaveGame != null)
                {
                    hasLoaded = false;
                    iSaveableData = null;
                    iSaveableDataIdentifiers.Clear();
                }

                cachedSaveGame = saveGame;
            }

            if (saveGame == null)
            {
                Debug.LogWarning("Invalid save game request");
                return;
            }

            if (loadOnce && hasLoaded)
            {
                return;
            }

            if (string.IsNullOrEmpty(saveIdentification))
            {
                Debug.LogWarning(string.Format("Save identification is empty on {0}", this.gameObject.name));
                return;
            }

            iSaveableData = JsonUtility.FromJson<ISaveableDataCollection>(saveGame.Get(saveIdentification));

            if (iSaveableData != null)
            {
                for (int i = 0; i < iSaveableData.saveStructures.Count; i++)
                {
                    // Try to get a saveable component by it's unique identifier.
                    ISaveable getSaveable;
                    saveableComponentDictionary.TryGetValue(iSaveableData.saveStructures[i].identifier, out getSaveable);

                    if (getSaveable != null)
                    {
                        getSaveable.OnLoad(iSaveableData.saveStructures[i].data);

                        if (!iSaveableDataIdentifiers.ContainsKey(iSaveableData.saveStructures[i].identifier))
                        {
                            iSaveableDataIdentifiers.Add(iSaveableData.saveStructures[i].identifier, i);
                        }
                    }
                }
            }
            else
            {
                iSaveableData = new ISaveableDataCollection();
            }

            hasLoaded = true;
        }
    }
}