using System.Collections.Generic;
using System.Linq;
using Lowscope.Saving.Data;
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
        private List<CachedSaveableComponent> cachedSaveableComponents = new List<CachedSaveableComponent>();

        //private Dictionary<string, ISaveable> saveableComponentDictionary = new Dictionary<string, ISaveable>();

        private List<string> saveableComponentIDs = new List<string>();
        private List<ISaveable> saveableComponentObjects = new List<ISaveable>();

        public string saveIdentification;

        private bool hasLoaded;

        /// <summary>
        /// Means of storing all saveable components for the ISaveable component.
        /// </summary>
        [System.Serializable]
        public class CachedSaveableComponent
        {
            public string identifier;
            public MonoBehaviour monoBehaviour;
        }

        private SaveGame cachedSaveGame;

        public bool ManualSaveLoad
        {
            get { return manualSaveLoad; }
            set { manualSaveLoad = value; }
        }

#if UNITY_EDITOR

        private static Dictionary<string, Saveable> saveIdentificationCache = new Dictionary<string, Saveable>();

        private static string identificationData;
        private static string lastSelectedGUID;

        private void SetIdentifcation(int index, string identifier)
        {
            cachedSaveableComponents[index].identifier = identifier;
        }

        public void OnValidate()
        {
            if (Application.isPlaying)
                return;

            bool isPrefab;

#if UNITY_2018_3_OR_NEWER 
            isPrefab = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this.gameObject);
#else
            isPrefab = this.gameObject.scene.name == null;
#endif

            // Set a new save identification if it is not a prefab at the moment.
            if (!isPrefab)
            {
                Lowscope.Tools.ValidateHierarchy.Add(this);

                bool isDuplicate = false;
                Saveable saveable = null;

                // Does the object have a valid save id? If not, we give a new one.
                if (!string.IsNullOrEmpty(saveIdentification))
                {
                    isDuplicate = saveIdentificationCache.TryGetValue(saveIdentification, out saveable);

                    if (!isDuplicate)
                    {
                        saveIdentificationCache.Add(saveIdentification, this);
                    }
                    else
                    {
                        if (saveable != this)
                        {
                            saveIdentification = "";
                        }
                    }
                }

                if (string.IsNullOrEmpty(saveIdentification))
                {
#if NET_4_6
                    saveIdentification = $"{gameObject.scene.name}-{gameObject.name}-{System.Guid.NewGuid().ToString().Substring(0, 5)}";
#else
                    saveIdentification = string.Format("{0}-{1}-{2}", gameObject.scene.name, gameObject.name, System.Guid.NewGuid().ToString().Substring(0, 5));
#endif
                    saveIdentificationCache.Add(saveIdentification, this);
                }
            }
            else
            {
                saveIdentification = string.Empty;
                UnityEditor.EditorUtility.SetDirty(this.gameObject);
            }

            List<ISaveable> obtainSaveables = new List<ISaveable>();

            obtainSaveables.AddRange(GetComponentsInChildren<ISaveable>(true).ToList());
            for (int i = 0; i < externalListeners.Count; i++)
            {
                if (externalListeners[i] != null)
                    obtainSaveables.AddRange(externalListeners[i].GetComponentsInChildren<ISaveable>(true).ToList());
            }

            for (int i = cachedSaveableComponents.Count - 1; i >= 0; i--)
            {
                if (cachedSaveableComponents[i].monoBehaviour == null)
                {
                    cachedSaveableComponents.RemoveAt(i);
                }
            }

            if (obtainSaveables.Count != cachedSaveableComponents.Count)
            {
                if (cachedSaveableComponents.Count > obtainSaveables.Count)
                {
                    for (int i = cachedSaveableComponents.Count - 1; i >= obtainSaveables.Count; i--)
                    {
                        cachedSaveableComponents.RemoveAt(i);
                    }
                }

                int saveableComponentCount = cachedSaveableComponents.Count;
                for (int i = saveableComponentCount - 1; i >= 0; i--)
                {
                    if (cachedSaveableComponents[i] == null)
                    {
                        cachedSaveableComponents.RemoveAt(i);
                    }
                }

                ISaveable[] cachedSaveables = new ISaveable[cachedSaveableComponents.Count];
                for (int i = 0; i < cachedSaveables.Length; i++)
                {
                    cachedSaveables[i] = cachedSaveableComponents[i].monoBehaviour as ISaveable;
                }

                ISaveable[] missingElements = obtainSaveables.Except(cachedSaveables).ToArray();

                for (int i = 0; i < missingElements.Length; i++)
                {
                    CachedSaveableComponent newSaveableComponent = new CachedSaveableComponent()
                    {
                        monoBehaviour = missingElements[i] as MonoBehaviour
                    };

                    string typeString = newSaveableComponent.monoBehaviour.GetType().Name.ToString();

                    var identifier = "";

                    while (!IsIdentifierUnique(identifier))
                    {
                        string guidString = System.Guid.NewGuid().ToString().Substring(0, 5);
                        identifier = string.Format("{0}-{1}", typeString, guidString);
                    }

                    newSaveableComponent.identifier = identifier;

                    cachedSaveableComponents.Add(newSaveableComponent);
                }

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(this.gameObject.scene);
            }
        }

        private bool IsIdentifierUnique(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            for (int i = 0; i < cachedSaveableComponents.Count; i++)
            {
                if (cachedSaveableComponents[i].identifier == identifier)
                {
                    return false;
                }
            }

            return true;
        }

        public void Refresh()
        {
            OnValidate();
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
            saveableComponentIDs.Add(string.Format("{0}-{1}", saveIdentification, identifier));
            saveableComponentObjects.Add(iSaveable);

            // Load it again, to ensure all ISaveable interfaces are updated.
            if (cachedSaveGame != null)
            {
                OnLoadRequest(cachedSaveGame);
            }
        }

        private void Awake()
        {
            // Store the component identifiers into a dictionary for performant retrieval.
            for (int i = 0; i < cachedSaveableComponents.Count; i++)
            {
                saveableComponentIDs.Add(string.Format("{0}-{1}", saveIdentification, cachedSaveableComponents[i].identifier));
                saveableComponentObjects.Add(cachedSaveableComponents[i].monoBehaviour as ISaveable);
            }

            if (!manualSaveLoad)
            {
                SaveMaster.AddListener(this);
            }
        }

        private void OnDestroy()
        {
            if (!manualSaveLoad)
            {
                SaveMaster.RemoveListener(this);
            }

#if UNITY_EDITOR
            Lowscope.Tools.ValidateHierarchy.Remove(this);
            saveIdentificationCache.Remove(saveIdentification);
#endif
        }

        /// <summary>
        /// Removes all save data related to this component.
        /// This is useful for dynamic saved objects. So they get erased
        /// from the save file permanently.
        /// </summary>
        public void WipeData()
        {
            if (cachedSaveGame != null)
            {
                int componentCount = saveableComponentIDs.Count;

                for (int i = componentCount - 1; i >= 0; i--)
                {
                    cachedSaveGame.Remove(saveableComponentIDs[i]);
                }

                // Ensures it doesn't try to save upon destruction.
                manualSaveLoad = true;
                SaveMaster.RemoveListener(this, false);
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

            int componentCount = saveableComponentIDs.Count;

            for (int i = componentCount - 1; i >= 0; i--)
            {
                ISaveable getSaveable = saveableComponentObjects[i];
                string getIdentification = saveableComponentIDs[i];

                if (getSaveable == null)
                {
                    Debug.Log(string.Format("Failed to save component: {0}. Component is potentially destroyed.", getIdentification));
                    saveableComponentIDs.RemoveAt(i);
                    saveableComponentObjects.RemoveAt(i);
                }
                else
                {
                    if (getSaveable.OnSaveCondition() == false)
                    {
                        continue;
                    }
                    else
                    {
                        saveGame.Set(getIdentification, getSaveable.OnSave());
                    }
                }
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

            int componentCount = saveableComponentIDs.Count;

            for (int i = componentCount - 1; i >= 0; i--)
            {
                ISaveable getSaveable = saveableComponentObjects[i];
                string getIdentification = saveableComponentIDs[i];

                if (getSaveable == null)
                {
                    Debug.Log(string.Format("Failed to load component: {0}. Component is potentially destroyed.", getIdentification));
                    saveableComponentIDs.RemoveAt(i);
                    saveableComponentObjects.RemoveAt(i);
                }
                else
                {
                    string getData = saveGame.Get(saveableComponentIDs[i]);

                    if (!string.IsNullOrEmpty(getData))
                    {
                        getSaveable.OnLoad(getData);
                    }
                }
            }
        }
    }
}