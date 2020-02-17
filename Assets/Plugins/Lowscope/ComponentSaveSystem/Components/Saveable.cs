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
        private bool loadOnce = false;

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

        [SerializeField] private string saveIdentification;
        public string SaveIdentification
        {
            get
            {
                return saveIdentification;
            }
            set
            {
                saveIdentification = value;
                hasIdentification = !string.IsNullOrEmpty(saveIdentification);
            }
        }

        private bool hasLoaded;
        private bool hasStateReset;
        private bool hasIdentification;

        /// <summary>
        /// Means of storing all saveable components for the ISaveable component.
        /// </summary>
        [System.Serializable]
        public class CachedSaveableComponent
        {
            public string identifier;
            public MonoBehaviour monoBehaviour;
        }

        public bool ManualSaveLoad
        {
            get { return manualSaveLoad; }
            set { manualSaveLoad = value; }
        }

#if UNITY_EDITOR

        // Used to check if you are duplicating an object. If so, it assigns a new identification
        private static Dictionary<string, Saveable> saveIdentificationCache = new Dictionary<string, Saveable>();

        // Used to prevent duplicating GUIDS when you copy a scene.
        [HideInInspector] [SerializeField] private string sceneName;

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

                if (sceneName != gameObject.scene.name)
                {
                    UnityEditor.Undo.RecordObject(this, "Updated Object Scene ID");

                    if (SaveSettings.Get().resetSaveableIdOnNewScene)
                    {
                        saveIdentification = "";
                    }

                    sceneName = gameObject.scene.name;
                }

                if (SaveSettings.Get().resetSaveableIdOnDuplicate)
                {
                    // Does the object have a valid save id? If not, we give a new one.
                    if (!string.IsNullOrEmpty(saveIdentification))
                    {
                        isDuplicate = saveIdentificationCache.TryGetValue(saveIdentification, out saveable);

                        if (!isDuplicate)
                        {
                            if (saveIdentification != "")
                            {
                                saveIdentificationCache.Add(saveIdentification, this);
                            }
                        }
                        else
                        {
                            if (saveable == null)
                            {
                                saveIdentificationCache.Remove(saveIdentification);
                                saveIdentificationCache.Add(saveIdentification, this);
                            }
                            else
                            {
                                if (saveable.gameObject != this.gameObject)
                                {
                                    UnityEditor.Undo.RecordObject(this, "Updated Object Scene ID");
                                    saveIdentification = "";
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(saveIdentification))
                {
                    UnityEditor.Undo.RecordObject(this, "ClearedSaveIdentification");

                    int guidLength = SaveSettings.Get().gameObjectGuidLength;

#if NET_4_6
                    saveIdentification = $"{gameObject.scene.name}-{gameObject.name}-{System.Guid.NewGuid().ToString().Substring(0, 5)}";
#else
                    saveIdentification = string.Format("{0}-{1}-{2}", gameObject.scene.name, gameObject.name, System.Guid.NewGuid().ToString().Substring(0, guidLength));
#endif
                    saveIdentificationCache.Add(saveIdentification, this);

                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(this.gameObject.scene);
                }
            }
            else
            {
                saveIdentification = string.Empty;
                sceneName = string.Empty;
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
                        int guidLength = SaveSettings.Get().componentGuidLength;
                        string guidString = System.Guid.NewGuid().ToString().Substring(0, guidLength);
                        identifier = string.Format("{0}-{1}", typeString, guidString);
                    }

                    newSaveableComponent.identifier = identifier;

                    cachedSaveableComponents.Add(newSaveableComponent);
                }

                UnityEditor.EditorUtility.SetDirty(this);
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

            // Load it again, to ensure all ISaveable interfaces are updated.
            SaveMaster.ReloadListener(this);
        }

        /// <summary>
        /// Useful if you want to dynamically add a saveable component. To ensure it 
        /// gets registered.
        /// </summary>
        /// <param name="identifier">The identifier for the component, this is the adress the data will be loaded from </param>
        /// <param name="iSaveable">The interface reference on the component. </param>
        /// <param name="reloadData">Do you want to reload the data on all the components? 
        /// Only call this if you add one component. Else call SaveMaster.ReloadListener(saveable). </param>
        public void AddSaveableComponent(string identifier, ISaveable iSaveable, bool reloadData = false)
        {
            saveableComponentIDs.Add(string.Format("{0}-{1}", saveIdentification, identifier));
            saveableComponentObjects.Add(iSaveable);

            if (reloadData)
            {
                // Load it again, to ensure all ISaveable interfaces are updated.
                SaveMaster.ReloadListener(this);
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
            if (!Application.isPlaying)
            {
                Lowscope.Tools.ValidateHierarchy.Remove(this);
                saveIdentificationCache.Remove(saveIdentification);
            }
#endif
        }

        /// <summary>
        /// Removes all save data related to this component.
        /// This is useful for dynamic saved objects. So they get erased
        /// from the save file permanently.
        /// </summary>
        public void WipeData(SaveGame saveGame)
        {
            int componentCount = saveableComponentIDs.Count;

            for (int i = componentCount - 1; i >= 0; i--)
            {
                saveGame.Remove(saveableComponentIDs[i]);
            }

            // Ensures it doesn't try to save upon destruction.
            manualSaveLoad = true;
            SaveMaster.RemoveListener(this, false);
        }

        /// <summary>
        /// Used to reset the saveable, as if it was never saved or loaded.
        /// </summary>
        public void ResetState()
        {
            // Since the game uses a new save game, reset loadOnce and hasLoaded
            loadOnce = false;
            hasLoaded = false;
            hasStateReset = true;
        }

        // Request is sent by the Save System
        public void OnSaveRequest(SaveGame saveGame)
        {
            if (!hasIdentification)
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
                    if (!hasStateReset && !getSaveable.OnSaveCondition())
                    {
                        continue;
                    }

                    string dataString = getSaveable.OnSave();

                    if (!string.IsNullOrEmpty(dataString))
                    {
                        saveGame.Set(getIdentification, dataString, this.gameObject.scene.name);
                    }
                }
            }

            hasStateReset = false;
        }

        // Request is sent by the Save System
        public void OnLoadRequest(SaveGame saveGame)
        {
            if (loadOnce && hasLoaded)
            {
                return;
            }
            else
            {
                // Ensure it only loads once with the loadOnce
                // Parameter
                hasLoaded = true;
                hasIdentification = !string.IsNullOrEmpty(saveIdentification);
            }

            if (!hasIdentification)
            {
                Debug.Log("No identification!");
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