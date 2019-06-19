using System;
using System.Collections;
using System.Collections.Generic;
using Lowscope.Saving.Components;
using Lowscope.Saving.Core;
using Lowscope.Saving.Data;
using Lowscope.Saving.Enums;
using UnityEngine;
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
        private static SaveInstanceManager saveInstanceManager;

        private static GameObject saveMasterTemplate;
        private static Dictionary<string, SaveInstanceManager> saveInstanceManagers;

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
            instance = saveMasterObject.AddComponent<SaveMaster>();

            saveInstanceManagers = new Dictionary<string, SaveInstanceManager>();

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
            if (saveInstanceManagers.ContainsKey(scene.name))
            {
                saveInstanceManagers.Remove(scene.name);
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            if (!saveInstanceManagers.ContainsKey(scene.name))
            {
                SpawnInstanceManager(scene);
            }
        }

        private static void SpawnInstanceManager(Scene scene)
        {
            // We spawn a game object seperately, so we can keep it disabled during configuration.
            // This prevents any UnityEngine calls such as Awake or Start
            var go = new GameObject("Save Instance Manager");
            go.gameObject.SetActive(false);

            var instanceManager = go.AddComponent<SaveInstanceManager>();
            var saveable = go.AddComponent<Saveable>();

            saveable.saveIdentification = string.Format("{0}-{1}", "SaveMaster", scene.name);
            saveable.AddSaveableComponent("IM", instanceManager);
            saveInstanceManager = instanceManager;

            saveInstanceManagers.Add(scene.name, saveInstanceManager);

            SceneManager.MoveGameObjectToScene(go, scene);

            go.gameObject.SetActive(true);
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
        public static bool SetSlotToFirstUnused(bool notifyListeners, out int slot)
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
        /// Will load the last used scenes for save game, and set the slot. 
        /// Current scene also gets saved, if any slot is currently set.
        /// If slot is empty, it will still set it, and load the default set starting scene.
        /// </summary>
        /// <param name="slot"> Save slot to load, it will create a new one if empty </param>
        /// <param name="defaultScene"> Scene to load in case the save is empty </param>
        /// <param name="defaultScene"> Additional scenes to load in case the save is empty </param>
        public static bool LoadSlot(int slot, string defaultScene = "")
        {
            // Ensure the current game is saved, and write it to disk, if that is wanted behaviour.
            if (SaveSettings.Get().autoSave && SaveSettings.Get().autoSaveOnSlotSwitch && activeSaveGame != null)
            {
                WriteActiveSaveToDisk();
            }

            bool slotExists = SaveFileUtility.IsSlotUsed(slot);

            string startScene = string.IsNullOrEmpty(defaultScene) ? SaveSettings.Get().defaultStartScene : defaultScene;

            // Default can also still be empty.
            if (!slotExists && string.IsNullOrEmpty(startScene))
            {
                SaveMaster.SetSlot(slot, false);
                //Debug.Log("Slot is empty: Please set a default starting scene.");
                return true;
            }

            SaveGame save = SaveFileUtility.LoadSave(slot, true);

            var activeScene = !slotExists ? startScene : save.lastActiveScene;

            if (SceneUtility.GetBuildIndexByScenePath(activeScene) == -1)
            {
                Debug.Log(string.Format("Attempted to load scene named {0}. This scene is not added in the build list",
                    activeScene));
                return false;
            }

            instance.StartCoroutine(LoadSlotCoroutine(slot, activeScene, save));
            return true;
        }

        // Coroutine is used to properly unload other scenes before switching saves.
        // This ensures the previous SaveGame has all data written. Since ISaveable components
        // Write to the SaveGame upon Destruction. Afterwards when the slot changes, and autosave is toggled on it 
        // will write that SaveGame to the disk.
        private static IEnumerator LoadSlotCoroutine(int slot, string activeScene, SaveGame saveGame)
        {
            // Get all scenes to unload
            Scene[] scenesToUnload = new Scene[SceneManager.sceneCount];
            AsyncOperation[] sceneUnloadActions = new AsyncOperation[SceneManager.sceneCount];

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                scenesToUnload[i] = SceneManager.GetSceneAt(i);
            }

            // Create temporary scene so it's possible to unload everything.
            // (Has to be done because of a Unity bug)
            SceneManager.CreateScene("-");

            for (int i = 0; i < scenesToUnload.Length; i++)
            {
                sceneUnloadActions[i] = SceneManager.UnloadSceneAsync(scenesToUnload[i]);
            }

            int actionCount = sceneUnloadActions.Length;

            while (true)
            {
                int loadCount = 0;
                for (int i = 0; i < actionCount; i++)
                {
                    if (sceneUnloadActions[i].isDone)
                    {
                        loadCount++;
                    }
                }

                if (loadCount >= actionCount - 1)
                {
                    break;
                }

                yield return null;
            }

            yield return null;

            SaveMaster.SetSlot(slot, false, saveGame);

            yield return null;

            var op = SceneManager.LoadSceneAsync(activeScene);
            op.allowSceneActivation = false;

            yield return new WaitUntil(() => op.progress == 0.9f);

            op.allowSceneActivation = true;
        }

        /// <summary>
        /// Set the active save slot
        /// </summary>
        /// <param name="slot"> Target save slot </param>
        /// <param name="notifyListeners"> Send a message to all saveables to load the new save file </param>
        public static void SetSlot(int slot, bool notifyListeners, SaveGame saveGame = null)
        {
            if (slot < 0 || slot > SaveSettings.Get().maxSaveSlotCount)
            {
                Debug.LogWarning("SaveMaster: Attempted to set illegal slot.");
                return;
            }

            activeSlot = slot;
            activeSaveGame = (saveGame == null) ? SaveFileUtility.LoadSave(slot, true) : saveGame;

            if (notifyListeners)
            {
                SyncLoad();
            }

            PlayerPrefs.SetInt("SM-LastUsedSlot", slot);
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

        public static int[] GetUsedSlots()
        {
            return SaveFileUtility.GetUsedSlots();
        }

        private static SaveGame GetSave(int slot, bool createIfEmpty = true)
        {
            if (slot == activeSlot)
            {
                return activeSaveGame;
            }

            return SaveFileUtility.LoadSave(slot, createIfEmpty);
        }

        public static bool IsSlotUsed(int slot)
        {
            return SaveFileUtility.IsSlotUsed(slot);
        }

        /// <summary>
        /// Automatically done on application quit or pause.
        /// Exposed in case you still want to manually write the active save.
        /// </summary>
        public static void WriteActiveSaveToDisk()
        {
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
        }

        /// <summary>
        /// Add saveable from the notification list. So it can recieve load/save requests.
        /// </summary>
        public static void AddListener(Saveable saveable)
        {
            if (saveable != null && activeSaveGame != null)
            {
                saveable.OnLoadRequest(activeSaveGame);
            }

            saveables.Add(saveable);
        }

        /// <summary>
        /// Remove saveable from the notification list. So it no longers recieves load/save requests.
        /// </summary>
        public static void RemoveListener(Saveable saveable)
        {
            if (saveable != null && activeSaveGame != null)
            {
                saveable.OnSaveRequest(activeSaveGame);
            }

            saveables.Remove(saveable);
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
                saveables.Clear();
            }
        }

        /// <summary>
        /// Removes the active save file. Based on the save slot index.
        /// </summary>
        public static void DeleteActiveSaveGame()
        {
            SaveFileUtility.DeleteSave(activeSlot);
            activeSlot = -1;
            activeSaveGame = null;
            saveables.Clear();
        }

        /// <summary>
        /// Wipes all data relevant to the given saveable component.
        /// </summary>
        /// <param name="saveable"></param>
        public static void WipeData(Saveable saveable)
        {
            if (saveable != null)
            {
                saveables.Remove(saveable);
                activeSaveGame.Remove(saveable.saveIdentification);
            }
            else
            {
                Debug.LogError("SaveMaster: Attempted to remove a null saveable reference");
            }
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

            string getDataCollection = saveGame.Get(saveableId);

            if (!string.IsNullOrEmpty(getDataCollection))
            {
                var dataCollection = JsonUtility.FromJson<ISaveableDataCollection>(getDataCollection);

                ISaveableData componentData;

                if (dataCollection.GetData(componentId, out componentData))
                {
                    if (!string.IsNullOrEmpty(componentData.data))
                    {
                        data = JsonUtility.FromJson<T>(componentData.data);
                        return true;
                    }
                }
            }

            data = default(T);
            return false;
        }

        public static GameObject SpawnObject(InstanceSource source, string filePath)
        {
            return HasActiveSave("Spawning Object") == false ?
                null : saveInstanceManager.SpawnObject(source, filePath);
        }

        /// <summary>
        /// Set a integer value in the current currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="value"> Value to store </param>
        public static void SetInt(string key, int value)
        {
            if (HasActiveSave("Set Int") == false) return;
            activeSaveGame.Set(string.Format("IVar-{0}", key), value.ToString());
        }

        /// <summary>
        /// Get a integer value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="defaultValue"> In case it fails to obtain the value, return this value </param>
        /// <returns> Stored value </returns>
        public static int GetInt(string key, int defaultValue = -1)
        {
            if (HasActiveSave("Get Int") == false) return defaultValue;
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
            if (HasActiveSave("Set Float") == false) return;
            activeSaveGame.Set(string.Format("FVar-{0}", key), value.ToString());
        }

        /// <summary>
        /// Get a float value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="defaultValue"> In case it fails to obtain the value, return this value </param>
        /// <returns> Stored value </returns>
        public static float GetFloat(string key, float defaultValue = -1)
        {
            if (HasActiveSave("Get Float") == false) return defaultValue;
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
            if (HasActiveSave("Set String") == false) return;
            activeSaveGame.Set(string.Format("SVar-{0}", key), value);
        }

        /// <summary>
        /// Get a string value in the currently active save
        /// </summary>
        /// <param name="key"> Identifier to remember storage point </param>
        /// <param name="defaultValue"> In case it fails to obtain the value, return this value </param>
        /// <returns> Stored value </returns>
        public static string GetString(string key, string defaultValue = "")
        {
            if (HasActiveSave("Get String") == false) return defaultValue;
            var getData = activeSaveGame.Get(string.Format("SVar-{0}", key));
            return string.IsNullOrEmpty((getData)) ? defaultValue : getData;
        }

        private static bool HasActiveSave(string action)
        {
            if (SaveMaster.GetActiveSlot() == -1)
            {
                Debug.LogWarning(string.Format("{0} Failed: no save slot set. Please call SetSaveSlot(int index)",
                    action));
                return false;
            }
            else return true;
        }

        private void Awake()
        {
            if (this == instance)
            {
                Debug.LogWarning("Duplicate save master found. " +
                                 "Ensure that the save master has not been added anywhere in your scene.");
                GameObject.Destroy(this.gameObject);
                return;
            }
        }

        private void Start()
        {
            var settings = SaveSettings.Get();

            if (settings.loadDefaultSlotOnStart)
            {
                SetSlot(settings.defaultSlot, true);
            }

            if (settings.trackTimePlayed)
            {
                StartCoroutine(IncrementTimePlayed());
            }

            if (settings.useSlotMenu || settings.useHotkeys)
            {
                StartCoroutine(TrackOpenSlotMenu());
            }
        }

        private IEnumerator TrackOpenSlotMenu()
        {
            var settings = SaveSettings.Get();
            GameObject instance = null;

            while (true)
            {
                yield return null;

                if (settings.useSlotMenu && Input.GetKeyDown(settings.openSlotMenuKey))
                {
                    if (instance == null)
                    {
                        instance = GameObject.Instantiate(settings.openSlotMenuPrefab);
                    }
                    else
                    {
                        instance.gameObject.SetActive(!instance.gameObject.activeSelf);
                    }
                }

                if (!settings.useHotkeys)
                {
                    continue;
                }

                if (Input.GetKeyDown(settings.saveGameKey))
                {
                    var stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    WriteActiveSaveToDisk();

                    stopWatch.Stop();
                    Debug.Log(string.Format("Synced objects & Witten game to disk. MS: {0}", stopWatch.ElapsedMilliseconds.ToString()));
                }

                if (Input.GetKeyDown(settings.loadGameKey))
                {
                    var stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    LoadSlot(activeSlot);

                    stopWatch.Stop();
                    Debug.Log(string.Format("Synced objects & Loaded game from disk. MS: {0}", stopWatch.ElapsedMilliseconds.ToString()));
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
            if (!SaveSettings.Get().autoSave)
                return;

            WriteActiveSaveToDisk();
        }

        private void OnApplicationQuit()
        {
            if (!SaveSettings.Get().autoSave)
                return;

            isQuittingGame = true;
            WriteActiveSaveToDisk();
        }
    }
}