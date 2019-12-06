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
        public static void ClearSlot()
        {
            activeSlot = -1;
            activeSaveGame = null;
        }

        /// <summary>
        /// Set the active save slot
        /// </summary>
        /// <param name="slot"> Target save slot </param>
        /// <param name="notifyListeners"> Send a message to all saveables to load the new save file </param>
        public static void SetSlot(int slot, bool notifyListeners, SaveGame saveGame = null)
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
        /// Wipe all data of a specified scene
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
                        saveables[i].WipeData();
                    }
                }
            }

            activeSaveGame.WipeSceneData(name);
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

        public static GameObject SpawnSavedPrefab(InstanceSource source, string filePath)
        {
            return HasActiveSave("Spawning Object") == false ?
                null : saveInstanceManager.SpawnObject(source, filePath);
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
            if (HasActiveSave("Set Int") == false) return;
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