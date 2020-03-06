using System;
using System.Collections;
using System.Collections.Generic;
using Lowscope.Saving.Components;
using Lowscope.Saving.Core;
using Lowscope.Saving.Data;
using Lowscope.Saving.Enums;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Lowscope.Saving
{
    /// <summary>
    /// Responsible for notifying all Saveable components
    /// Asking them to send data or retrieve data from/to the SaveGame
    /// </summary>
    [AddComponentMenu(""), DefaultExecutionOrder(-9015)]
    public class SaveMaster : MonoBehaviour
    {
        private static SaveMaster instance;

        private static GameObject saveMasterTemplate;

        // Used to track duplicate scenes.
        private static Dictionary<string, int> loadedSceneNames = new Dictionary<string, int>();
        private static HashSet<int> duplicatedSceneHandles = new HashSet<int>();

        private static Dictionary<int, SaveInstanceManager> saveInstanceManagers
            = new Dictionary<int, SaveInstanceManager>();

        private static bool isQuittingGame;

        // Active save game data
        private static SaveGame activeSaveGame = null;
        private static int activeSlot = -1;

        // All listeners
        private static List<Saveable> saveables = new List<Saveable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            GameObject saveMasterObject = new GameObject("Save Master");
            saveMasterObject.AddComponent<SaveMaster>();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            GameObject.DontDestroyOnLoad(saveMasterObject);
        }

        /*  
        *  Instance managers exist to keep track of spawned objects.
        *  These managers make it possible to drop a coin, and when you reload the game
        *  the coin will still be there.
        */

        private static void OnSceneUnloaded(Scene scene)
        {
            if (activeSaveGame == null)
                return;

            // If it is a duplicate scene, we just remove this handle.
            if (duplicatedSceneHandles.Contains(scene.GetHashCode()))
            {
                duplicatedSceneHandles.Remove(scene.GetHashCode());
            }
            else
            {
                if (loadedSceneNames.ContainsKey(scene.name))
                {
                    loadedSceneNames.Remove(scene.name);
                }
            }

            if (saveInstanceManagers.ContainsKey(scene.GetHashCode()))
            {
                saveInstanceManagers.Remove(scene.GetHashCode());
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            if (activeSaveGame == null)
                return;

            // Store a refeference to a non-duplicate scene
            if (!loadedSceneNames.ContainsKey(scene.name))
            {
                loadedSceneNames.Add(scene.name, scene.GetHashCode());
            }
            else
            {
                // These scenes are marked as duplicates. They need special treatment for saving.
                duplicatedSceneHandles.Add(scene.GetHashCode());
            }

            // Dont create save instance manager if there are no saved instances in the scene.
            if (string.IsNullOrEmpty(activeSaveGame.Get(string.Format("SaveMaster-{0}-IM", scene.name))))
            {
                return;
            }

            if (!saveInstanceManagers.ContainsKey(scene.GetHashCode()))
            {
                var instanceManager = SpawnInstanceManager(scene);
            }
        }

        /// <summary>
        /// You only need to call this for scenes with a duplicate name. If you have a duplicate ID, you can then 
        /// assign a ID to it. And it will save the data of the saveable to that ID instead.
        /// </summary>
        /// <param name="scene">  </param>
        /// <param name="id"> Add a extra indentification for the scene. Useful for duplicated scenes. </param>
        /// <returns></returns>
        public static SaveInstanceManager SpawnInstanceManager(Scene scene, string id = "")
        {
            // Safety precautions.
            if (!string.IsNullOrEmpty(id) && duplicatedSceneHandles.Contains(scene.GetHashCode()))
            {
                duplicatedSceneHandles.Remove(scene.GetHashCode());
            }

            // Already exists
            if (saveInstanceManagers.ContainsKey(scene.GetHashCode()))
            {
                return null;
            }

            // We spawn a game object seperately, so we can keep it disabled during configuration.
            // This prevents any UnityEngine calls such as Awake or Start
            var go = new GameObject("Save Instance Manager");
            go.gameObject.SetActive(false);

            var instanceManager = go.AddComponent<SaveInstanceManager>();
            var saveable = go.AddComponent<Saveable>();
            SceneManager.MoveGameObjectToScene(go, scene);

            string saveID = string.IsNullOrEmpty(id) ? scene.name : string.Format("{0}-{1}", scene.name, id);

            saveable.SaveIdentification = string.Format("{0}-{1}", "SaveMaster", saveID);
            saveable.AddSaveableComponent("IM", instanceManager, true);
            saveInstanceManagers.Add(scene.GetHashCode(), instanceManager);

            instanceManager.SceneID = saveID;
            instanceManager.Saveable = saveable;

            go.gameObject.SetActive(true);
            return instanceManager;
        }

        /// <summary>
        /// Returns if the object has been destroyed using GameObject.Destroy(obj).
        /// Will return false if it has been destroyed due to the game exitting or scene unloading.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool DeactivatedObjectExplicitly(GameObject gameObject)
        {
            return gameObject.scene.isLoaded && !SaveMaster.isQuittingGame;
        }

        /// <summary>
        /// Returns the active slot. -1 means no slot is loaded
        /// </summary>
        /// <returns> Active slot </returns>
        public static int GetActiveSlot()
        {
            return activeSlot;
        }

        /// <summary>
        /// Checks if there are any unused save slots.
        /// </summary>
        /// <returns></returns>
        public static bool HasUnusedSlots()
        {
            return SaveFileUtility.GetAvailableSaveSlot() != -1;
        }

        public static int[] GetUsedSlots()
        {
            return SaveFileUtility.GetUsedSlots();
        }

        public static bool IsSlotUsed(int slot)
        {
            return SaveFileUtility.IsSlotUsed(slot);
        }

        /// <summary>
        /// Tries to set the current slot to the last used one.
        /// </summary>
        /// <param name="notifyListeners"> Should a load event be send to all active Saveables?</param>
        /// <returns> If it was able to set the slot to the last used one </returns>
        public static bool SetSlotToLastUsedSlot(bool notifyListeners)
        {
            int lastUsedSlot = PlayerPrefs.GetInt("SM-LastUsedSlot", -1);

            if (lastUsedSlot == -1)
            {
                return false;
            }
            else
            {
                SetSlot(lastUsedSlot, notifyListeners);
                return true;
            }
        }

        /// <summary>
        /// Attempts to set the slot to the first unused slot. Useful for creating a new game.
        /// </summary>
        /// <param name="notifyListeners"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static bool SetSlotToNewSlot(bool notifyListeners, out int slot)
        {
            int availableSlot = SaveFileUtility.GetAvailableSaveSlot();

            if (availableSlot == -1)
            {
                slot = -1;
                return false;
            }
            else
            {
                SetSlot(availableSlot, notifyListeners);
                slot = availableSlot;
                return true;
            }
        }

        /// <summary>
        /// Ensure save master has not been set to any slot
        /// </summary>
        public static void ClearSlot(bool clearAllListeners = true, bool notifySave = true)
        {
            if (clearAllListeners)
            {
                ClearListeners(notifySave);
            }

            activeSlot = -1;
            activeSaveGame = null;
        }

        /// <summary>
        /// Sets the slot, but does not save the data in the previous slot. This is useful if you want to
        /// save the active game to a new save slot. Like in older games such as Half-Life.
        /// </summary>
        /// <param name="slot"> Slot to switch towards, and copy the current save to </param>
        /// <param name="saveGame"> Set this if you want to overwrite a specific save file </param>
        public static void SetSlotAndCopyActiveSave(int slot)
        {
            OnSlotChangeBegin.Invoke(slot);

            activeSlot = slot;
            activeSaveGame = SaveFileUtility.LoadSave(slot, true);

            SyncReset();
            SyncSave();

            OnSlotChangeDone.Invoke(slot);
        }

        /// <summary>
        /// Set the active save slot. (Do note: If you don't want to auto save on slot switch, you can change this in the save setttings)
        /// </summary>
        /// <param name="slot"> Target save slot </param>
        /// <param name="reloadSaveables"> Send a message to all saveables to load the new save file </param>
        public static void SetSlot(int slot, bool reloadSaveables, SaveGame saveGame = null)
        {
            if (activeSlot == slot && saveGame == null)
            {
                Debug.LogWarning("Already loaded this slot.");
                return;
            }

            // Ensure the current game is saved, and write it to disk, if that is wanted behaviour.
            if (SaveSettings.Get().autoSaveOnSlotSwitch && activeSaveGame != null)
            {
                WriteActiveSaveToDisk();
            }

            if (SaveSettings.Get().cleanSavedPrefabsOnSlotSwitch)
            {
                ClearActiveSavedPrefabs();
            }

            if (slot < 0 || slot > SaveSettings.Get().maxSaveSlotCount)
            {
                Debug.LogWarning("SaveMaster: Attempted to set illegal slot.");
                return;
            }

            OnSlotChangeBegin.Invoke(slot);

            activeSlot = slot;
            activeSaveGame = (saveGame == null) ? SaveFileUtility.LoadSave(slot, true) : saveGame;

            if (reloadSaveables)
            {
                SyncLoad();
            }

            SyncReset();

            PlayerPrefs.SetInt("SM-LastUsedSlot", slot);

            OnSlotChangeDone.Invoke(slot);
        }

        public static DateTime GetSaveCreationTime(int slot)
        {
            if (slot == activeSlot)
            {
                return activeSaveGame.creationDate;
            }

            if (!IsSlotUsed(slot))
            {
                return new DateTime();
            }

            return GetSave(slot, true).creationDate;
        }

        public static DateTime GetSaveCreationTime()
        {
            return GetSaveCreationTime(activeSlot);
        }

        public static TimeSpan GetSaveTimePlayed(int slot)
        {
            if (slot == activeSlot)
            {
                return activeSaveGame.timePlayed;
            }

            if (!IsSlotUsed(slot))
            {
                return new TimeSpan();
            }

            return GetSave(slot, true).timePlayed;
        }

        public static TimeSpan GetSaveTimePlayed()
        {
            return GetSaveTimePlayed(activeSlot);
        }

        public static int GetSaveVersion(int slot)
        {
            if (slot == activeSlot)
            {
                return activeSaveGame.gameVersion;
            }

            if (!IsSlotUsed(slot))
            {
                return -1;
            }

            return GetSave(slot, true).gameVersion;
        }

        public static int GetSaveVersion()
        {
            return GetSaveVersion(activeSlot);
        }

        private static SaveGame GetSave(int slot, bool createIfEmpty = true)
        {
            if (slot == activeSlot)
            {
                return activeSaveGame;
            }

            return SaveFileUtility.LoadSave(slot, createIfEmpty);
        }

        /// <summary>
        /// Automatically done on application quit or pause.
        /// Exposed in case you still want to manually write the active save.
        /// </summary>
        public static void WriteActiveSaveToDisk()
        {
            OnWritingToDiskBegin.Invoke(activeSlot);

            if (activeSaveGame != null)
            {
                for (int i = 0; i < saveables.Count; i++)
                {
                    saveables[i].OnSaveRequest(activeSaveGame);
                }

                SaveFileUtility.WriteSave(activeSaveGame, activeSlot);
            }
            else
            {
                if (Time.frameCount != 0)
                {
                    Debug.Log("No save game is currently loaded... So we cannot save it");
                }
            }

            OnWritingToDiskDone.Invoke(activeSlot);
        }

        /// <summary>
        /// Wipe all data of a specified scene. This is useful if you want to reset the saved state of a specific scene.
        /// Use clearSceneSaveables = true, in case you want to clear it before switching scenes.
        /// </summary>
        /// <param name="name"> Name of the scene </param>
        /// <param name="clearSceneSaveables"> Scan and wipe for any saveable in the scene? Else they might save again upon destruction.
        /// You can leave this off for performance if you are certain no active saveables are in the scene.</param>
        public static void WipeSceneData(string name, bool clearSceneSaveables = true)
        {
            if (activeSaveGame == null)
            {
                Debug.LogError("Failed to wipe scene data: No save game loaded.");
                return;
            }

            if (clearSceneSaveables)
            {
                int listenerCount = saveables.Count;
                for (int i = listenerCount - 1; i >= 0; i--)
                {
                    if (saveables[i].gameObject.scene.name == name)
                    {
                        saveables[i].WipeData(activeSaveGame);
                    }
                }
            }

            activeSaveGame.WipeSceneData(name);
        }

        /// <summary>
        /// Wipe all data of a specified saveable
        /// </summary>
        /// <param name="saveable"></param>
        public static void WipeSaveable(Saveable saveable)
        {
            if (activeSaveGame == null)
            {
                Debug.LogError("Failed to wipe scene data: No save game loaded.");
                return;
            }

            saveable.WipeData(activeSaveGame);
        }

        /// <summary>
        /// Clears all saveable components that are listening to the Save Master
        /// </summary>
        /// <param name="notifySave"></param>
        public static void ClearListeners(bool notifySave)
        {
            if (notifySave && activeSaveGame != null)
            {
                int saveableCount = saveables.Count;
                for (int i = saveableCount - 1; i >= 0; i--)
                {
                    saveables[i].OnSaveRequest(activeSaveGame);
                }
            }

            saveables.Clear();
        }

        /// <summary>
        /// Useful in case components have been added to a saveable.
        /// </summary>
        /// <param name="saveable"></param>
        public static void ReloadListener(Saveable saveable)
        {
            saveable.OnLoadRequest(activeSaveGame);
        }

        /// <summary>
        /// Add saveable from the notification list. So it can recieve load/save requests.
        /// </summary>
        /// <param name="saveable"> Reference to the saveable that listens to the Save Master </param>
        public static void AddListener(Saveable saveable)
        {
            if (saveable != null && activeSaveGame != null)
            {
                saveable.OnLoadRequest(activeSaveGame);
            }

            saveables.Add(saveable);
        }

        /// <summary>
        /// Add saveable from the notification list. So it can recieve load/save requests.
        /// </summary>
        /// <param name="saveable"> Reference to the saveable that listens to the Save Master </param>
        public static void AddListener(Saveable saveable, bool loadData)
        {
            if (loadData)
            {
                AddListener(saveable);
            }
            else
            {
                saveables.Add(saveable);
            }
        }

        /// <summary>
        /// Remove saveable from the notification list. So it no longers recieves load/save requests.
        /// </summary>
        /// <param name="saveable"> Reference to the saveable that listens to the Save Master </param>
        public static void RemoveListener(Saveable saveable)
        {
            if (saveables.Remove(saveable))
            {
                if (saveable != null && activeSaveGame != null)
                {
                    saveable.OnSaveRequest(activeSaveGame);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveable"> Reference to the saveable that listens to the Save Master </param>
        /// <param name="saveData"> Should it try to save the saveable data to the save file when being removed? </param>
        public static void RemoveListener(Saveable saveable, bool saveData)
        {
            if (saveData)
            {
                RemoveListener(saveable);
            }
            else
            {
                saveables.Remove(saveable);
            }
        }

        /// <summary>
        /// Delete a save file based on a specific slot.
        /// </summary>
        /// <param name="slot"></param>
        public static void DeleteSave(int slot)
        {
            SaveFileUtility.DeleteSave(slot);

            if (slot == activeSlot)
            {
                activeSlot = -1;
                activeSaveGame = null;
            }
        }

        /// <summary>
        /// Removes the active save file. Based on the save slot index.
        /// </summary>
        public static void DeleteSave()
        {
            DeleteSave(activeSlot);
        }

        /// <summary>
        /// Sends request to all saveables to store data to the active save game
        /// </summary>
        public static void SyncSave()
        {
            if (activeSaveGame == null)
            {
                Debug.LogWarning("SaveMaster Request Save Failed: " +
                                 "No active SaveGame has been set. Be sure to call SetSaveGame(index)");
                return;
            }

            int count = saveables.Count;

            for (int i = 0; i < count; i++)
            {
                saveables[i].OnSaveRequest(activeSaveGame);
            }
        }

        /// <summary>
        /// Sends request to all saveables to load data from the active save game
        /// </summary>
        public static void SyncLoad()
        {
            if (activeSaveGame == null)
            {
                Debug.LogWarning("SaveMaster Request Load Failed: " +
                                 "No active SaveGame has been set. Be sure to call SetSlot(index)");
                return;
            }

            int count = saveables.Count;

            for (int i = 0; i < count; i++)
            {
                saveables[i].OnLoadRequest(activeSaveGame);
            }
        }

        /// <summary>
        /// Resets the state of the saveables. As if they have never loaded or saved.
        /// </summary>
        public static void SyncReset()
        {
            if (activeSaveGame == null)
            {
                Debug.LogWarning("SaveMaster Request Load Failed: " +
                                 "No active SaveGame has been set. Be sure to call SetSlot(index)");
                return;
            }

            int count = saveables.Count;

            for (int i = 0; i < count; i++)
            {
                saveables[i].ResetState();
            }
        }

        /// <summary>
        /// Spawn a prefab that will be tracked & saved for a specific scene.
        /// </summary>
        /// <param name="source">Methodology to know where prefab came from </param>
        /// <param name="filePath">This is used to retrieve the prefab again from the designated source. </param>
        /// <param name="scene">Saved prefabs are bound to a specific scene. Easiest way to reference is by passing through (gameObject.scene).
        /// By default is uses the active scene. </param>
        /// <returns> Instance of saved prefab. </returns>
        public static GameObject SpawnSavedPrefab(InstanceSource source, string filePath, Scene scene = default(Scene))
        {
            if (!HasActiveSaveLogAction("Spawning Object"))
            {
                return null;
            }

            // If no scene has been specified, it will use the current active scene.
            if (scene == default(Scene))
            {
                scene = SceneManager.GetActiveScene();
            } 

            if (duplicatedSceneHandles.Contains(scene.GetHashCode()))
            {
                Debug.Log(string.Format("Following scene has a duplicate name: {0}. " +
                    "Ensure to call SaveMaster.SpawnInstanceManager(scene, id) with a custom ID after spawning the scene.", scene.name));
                scene = SceneManager.GetActiveScene();
            }

            SaveInstanceManager saveIM;
            if (!saveInstanceManagers.TryGetValue(scene.GetHashCode(), out saveIM))
            {
                saveIM = SpawnInstanceManager(scene);
            }

            return saveIM.SpawnObject(source, filePath).gameObject;
        }

        /// <summary>
        /// Helper method for obtaining specific Saveable data.
        /// </summary>
        /// <typeparam name="T"> Object type to retrieve </typeparam>
        /// <param name="classType">Object type to retrieve</param>
        /// <param name="slot"> Save slot to load data from </param>
        /// <param name="saveableId"> Identification of saveable </param>
        /// <param name="componentId"> Identification of saveable component </param>
        /// <param name="data"> Data that gets returned </param>
        /// <returns></returns>
        public static bool GetSaveableData<T>(int slot, string saveableId, string componentId, out T data)
        {
            if (IsSlotUsed(slot) == false)
            {
                data = default(T);
                return false;
            }

            SaveGame saveGame = SaveMaster.GetSave(slot, false);

            if (saveGame == null)
            {
                data = default(T);
                return false;
            }

            string dataString = saveGame.Get(string.Format("{0}-{1}", saveableId, componentId));

            if (!string.IsNullOrEmpty(dataString))
            {
                data = JsonUtility.FromJson<T>(dataString);

                if (data != null)
                    return true;
            }

            data = default(T);
            return false;
        }

        /// <summary>
        /// Helper method for obtaining specific Saveable data.
        /// </summary>
        /// <typeparam name="T"> Object type to retrieve </typeparam>
        /// <param name="classType">Object type to retrieve</param>
        /// <param name="saveableId"> Identification of saveable </param>
        /// <param name="componentId"> Identification of saveable component </param>
        /// <param name="data"> Data that gets returned </param>
        /// <returns></returns>
        public static bool GetSaveableData<T>(string saveableId, string componentId, out T data)
        {
            if (activeSlot == -1)
            {
                data = default(T);
                return false;
            }

            return GetSaveableData<T>(activeSlot, saveableId, componentId, out data);
        }

        /// <summary>
        /// Set a integer value in the current currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="value"> Value to store </param>
        public static void SetInt(string key, int value)
        {
            if (HasActiveSaveLogAction("Set Int") == false) return;
            activeSaveGame.Set(string.Format("IVar-{0}", key), value.ToString(), "Global");
        }

        /// <summary>
        /// Get a integer value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="defaultValue"> In case it fails to obtain the value, return this value </param>
        /// <returns> Stored value </returns>
        public static int GetInt(string key, int defaultValue = -1)
        {
            if (HasActiveSaveLogAction("Get Int") == false) return defaultValue;
            var getData = activeSaveGame.Get(string.Format("IVar-{0}", key));
            return string.IsNullOrEmpty((getData)) ? defaultValue : int.Parse(getData);
        }

        /// <summary>
        /// Set a floating point value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier for value </param>
        /// <param name="value"> Value to store </param>
        public static void SetFloat(string key, float value)
        {
            if (HasActiveSaveLogAction("Set Float") == false) return;
            activeSaveGame.Set(string.Format("FVar-{0}", key), value.ToString(), "Global");
        }

        /// <summary>
        /// Get a float value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="defaultValue"> In case it fails to obtain the value, return this value </param>
        /// <returns> Stored value </returns>
        public static float GetFloat(string key, float defaultValue = -1)
        {
            if (HasActiveSaveLogAction("Get Float") == false) return defaultValue;
            var getData = activeSaveGame.Get(string.Format("FVar-{0}", key));
            return string.IsNullOrEmpty((getData)) ? defaultValue : float.Parse(getData);
        }

        /// <summary>
        /// Set a string value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier for value </param>
        /// <param name="value"> Value to store </param>
        public static void SetString(string key, string value)
        {
            if (HasActiveSaveLogAction("Set String") == false) return;
            activeSaveGame.Set(string.Format("SVar-{0}", key), value, "Global");
        }

        /// <summary>
        /// Get a string value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="defaultValue"> In case it fails to obtain the value, return this value </param>
        /// <returns> Stored value </returns>
        public static string GetString(string key, string defaultValue = "")
        {
            if (HasActiveSaveLogAction("Get String") == false) return defaultValue;
            var getData = activeSaveGame.Get(string.Format("SVar-{0}", key));
            return string.IsNullOrEmpty((getData)) ? defaultValue : getData;
        }

        private static bool HasActiveSaveLogAction(string action)
        {
            if (SaveMaster.GetActiveSlot() == -1)
            {
                Debug.LogWarning(string.Format("{0} Failed: no save slot set. Please call SetSaveSlot(int index)",
                    action));
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Clean all currently saved prefabs. Useful when switching scenes.
        /// </summary>
        private static void ClearActiveSavedPrefabs()
        {
            int totalLoadedScenes = SceneManager.sceneCount;

            for (int i = 0; i < totalLoadedScenes; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                SaveInstanceManager saveIM;

                if (saveInstanceManagers.TryGetValue(scene.GetHashCode(), out saveIM))
                {
                    saveIM.DestroyAllObjects();
                }
            }
        }

        // Events

        /// <summary>
        /// Gets called after current saveables gets saved and written to disk.
        /// You can start loading scenes based on this callback.
        /// </summary>
        public static System.Action<int> OnSlotChangeBegin
        {
            get { return instance.onSlotChangeBegin; }
            set { instance.onSlotChangeBegin = value; }
        }

        public static System.Action<int> OnSlotChangeDone
        {
            get { return instance.onSlotChangeDone; }
            set { instance.onSlotChangeDone = value; }
        }

        public static System.Action<int> OnWritingToDiskBegin
        {
            get { return instance.onWritingToDiskBegin; }
            set { instance.onWritingToDiskBegin = value; }
        }

        public static System.Action<int> OnWritingToDiskDone
        {
            get { return instance.onWritingToDiskDone; }
            set { instance.onWritingToDiskDone = value; }
        }

        private System.Action<int> onSlotChangeBegin = delegate { };
        private System.Action<int> onSlotChangeDone = delegate { };
        private System.Action<int> onWritingToDiskBegin = delegate { };
        private System.Action<int> onWritingToDiskDone = delegate { };

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning("Duplicate save master found. " +
                                 "Ensure that the save master has not been added anywhere in your scene.");
                GameObject.Destroy(this.gameObject);
                return;
            }

            instance = this;

            var settings = SaveSettings.Get();

            if (settings.loadDefaultSlotOnStart)
            {
                SetSlot(settings.defaultSlot, true);
            }

            if (settings.trackTimePlayed)
            {
                StartCoroutine(IncrementTimePlayed());
            }

            if (settings.useHotkeys)
            {
                StartCoroutine(TrackHotkeyUsage());
            }
        }

        private IEnumerator TrackHotkeyUsage()
        {
            var settings = SaveSettings.Get();

            while (true)
            {
                yield return null;

                if (!settings.useHotkeys)
                {
                    continue;
                }

                if (Input.GetKeyDown(settings.wipeActiveSceneData))
                {
                    SaveMaster.WipeSceneData(SceneManager.GetActiveScene().name);
                }

                if (Input.GetKeyDown(settings.saveAndWriteToDiskKey))
                {
                    var stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    WriteActiveSaveToDisk();

                    stopWatch.Stop();
                    Debug.Log(string.Format("Synced objects & Witten game to disk. MS: {0}", stopWatch.ElapsedMilliseconds.ToString()));
                }

                if (Input.GetKeyDown(settings.syncSaveGameKey))
                {
                    var stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    SyncSave();

                    stopWatch.Stop();
                    Debug.Log(string.Format("Synced (Save) objects. MS: {0}", stopWatch.ElapsedMilliseconds.ToString()));
                }

                if (Input.GetKeyDown(settings.syncLoadGameKey))
                {
                    var stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    SyncLoad();

                    stopWatch.Stop();
                    Debug.Log(string.Format("Synced (Load) objects. MS: {0}", stopWatch.ElapsedMilliseconds.ToString()));
                }
            }
        }

        private IEnumerator IncrementTimePlayed()
        {
            WaitForSeconds incrementSecond = new WaitForSeconds(1);

            while (true)
            {
                yield return incrementSecond;

                if (activeSlot >= 0)
                {
                    activeSaveGame.timePlayed = activeSaveGame.timePlayed.Add(TimeSpan.FromSeconds(1));
                }
            }
        }

        // This will get called on android devices when they leave the game
        private void OnApplicationPause(bool pause)
        {
            if (!SaveSettings.Get().autoSaveOnExit)
                return;

            WriteActiveSaveToDisk();
        }

        private void OnApplicationQuit()
        {
            if (!SaveSettings.Get().autoSaveOnExit)
                return;

            isQuittingGame = true;
            WriteActiveSaveToDisk();
        }
    }
}