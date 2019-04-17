using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lowscope.Saving.Data;
using UnityEngine;

namespace Lowscope.Saving.Core
{
    public class SaveFileUtility
    {
        // Saving with WebGL requires a seperate DLL, which is included in the plugin.
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void SyncFiles();

    [DllImport("__Internal")]
    private static extern void WindowAlert(string message);
#endif

        private static string FileExtensionName { get { return SaveSettings.Get().fileExtensionName; } }
        private static string gameFileName { get { return SaveSettings.Get().fileName; } }

        private static bool debugMode { get { return SaveSettings.Get().showSaveFileUtilityLog; } }

        private static string DataPath
        {
            get
            {
                return string.Format("{0}/{1}",
                    Application.persistentDataPath,
                    SaveSettings.Get().fileFolderName);
            }
        }

#if UNITY_EDITOR
        private static void Log(string text)
        {
            if (debugMode)
            {
                Debug.Log(text);
            }
        }
#endif

        private static Dictionary<int, SaveGame> cachedSaveGames;

        public static Dictionary<int, SaveGame> ObtainAllSaveGames()
        {
            if (cachedSaveGames != null)
            {
                return cachedSaveGames;
            }

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            string[] filePaths = Directory.GetFiles(DataPath);

            string[] savePaths = filePaths.Where(path => path.EndsWith(FileExtensionName)).ToArray();

            Dictionary<int, SaveGame> gameSaves = new Dictionary<int, SaveGame>();

            for (int i = 0; i < savePaths.Length; i++)
            {
#if UNITY_EDITOR
                Log(string.Format("Found save at: {0}", savePaths[i]));
#endif

                using (var reader = new BinaryReader(File.Open(savePaths[i], FileMode.Open)))
                {
                    string dataString = reader.ReadString();

                    if (!string.IsNullOrEmpty(dataString))
                    {
                        LoadSaveFromPath(savePaths[i], ref gameSaves, dataString);
                    }
                    else
                    {
#if UNITY_EDITOR
                        Log(string.Format("Save file empty: {0}", savePaths[i]));
#endif
                    }
                }
            }

            cachedSaveGames = gameSaves;

            return gameSaves;
        }

        private static void LoadSaveFromPath(string savePath, ref Dictionary<int, SaveGame> gameSaves, string dataString)
        {
            SaveGame getSave = JsonUtility.FromJson<SaveGame>(dataString);

            if (getSave != null)
            {
                getSave.OnLoad();

                int getSlotNumber;

                string fileName = savePath.Substring(DataPath.Length + gameFileName.Length + 1);

                if (int.TryParse(fileName.Substring(0, fileName.LastIndexOf(".")), out getSlotNumber))
                {
                    gameSaves.Add(getSlotNumber, getSave);
                }
            }
            else
            {
#if UNITY_EDITOR
                Log(string.Format("Save file corrupted: {0}", savePath));
#endif
            }
        }

        public static int[] GetUsedSlots()
        {
            int[] saves = new int[ObtainAllSaveGames().Count];

            int counter = 0;

            foreach (int item in ObtainAllSaveGames().Keys)
            {
                saves[counter] = item;
                counter++;
            }

            return saves;
        }

        public static int GetSaveSlotCount()
        {
            return ObtainAllSaveGames().Count;
        }

        public static SaveGame LoadSave(int slot, bool createIfEmpty = false)
        {
            if (slot < 0)
            {
                Debug.LogWarning("Attempted to load negative slot");
                return null;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
                SyncFiles();
#endif

            SaveGame getSave;

            if (SaveFileUtility.ObtainAllSaveGames().TryGetValue(slot, out getSave))
            {
#if UNITY_EDITOR
                Log(string.Format("Succesful load at slot (from cache): {0}", slot));
#endif
                return getSave;
            }
            else
            {
                if (!createIfEmpty)
                {
#if UNITY_EDITOR
                    Log(string.Format("Could not load game at slot {0}", slot));
#endif
                }
                else
                {

#if UNITY_EDITOR
                    Log(string.Format("Creating save at slot {0}", slot));
#endif

                    SaveGame saveGame = new SaveGame();

                    WriteSave(saveGame, slot);

                    return saveGame;
                }

                return null;
            }
        }

        public static void WriteSave(SaveGame saveGame, int saveSlot)
        {
#if UNITY_EDITOR
            if (SaveSettings.Get().dontWriteSaveFiles)
            {
                return;
            }
#endif

            if (cachedSaveGames == null)
            {
                cachedSaveGames = new Dictionary<int, SaveGame>();
            }

            if (!cachedSaveGames.ContainsKey(saveSlot))
            {
                cachedSaveGames.Add(saveSlot, saveGame);
            }

            string savePath = string.Format("{0}/{1}{2}{3}", DataPath, gameFileName, saveSlot.ToString(), FileExtensionName);

#if UNITY_EDITOR
            Log(string.Format("Saving game slot {0} to : {1}", saveSlot.ToString(), savePath));
#endif
            saveGame.OnWrite();

            using (var writer = new BinaryWriter(File.Open(savePath, FileMode.Create)))
            {
                writer.Write(JsonUtility.ToJson(saveGame));
            }

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif

        }

        public static void DeleteSave(SaveGame saveGame)
        {
            int? removeIndex = ObtainAllSaveGames().FirstOrDefault(save => save.Value == saveGame).Key;

            if (removeIndex.HasValue)
            {
                DeleteSave(removeIndex.Value);
            }
        }

        public static void DeleteSave(int slot)
        {
            string filePath = string.Format("{0}/{1}{2}{3}", DataPath, gameFileName, slot, FileExtensionName);

            if (File.Exists(filePath))
            {
#if UNITY_EDITOR
                Log(string.Format("Succesfully removed file at {0}", filePath));
#endif
                File.Delete(filePath);

                if (cachedSaveGames.ContainsKey(slot))
                {
                    cachedSaveGames.Remove(slot);
                }
            }
            else
            {
#if UNITY_EDITOR
                Log(string.Format("Failed to remove file at {0}", filePath));
#endif
            }

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif
        }

        public static bool IsSlotUsed(int index)
        {
            return ObtainAllSaveGames().ContainsKey(index);
        }

        public static int GetAvailableSaveSlot()
        {
            int slotCount = SaveSettings.Get().maxSaveSlotCount;

            for (int i = 0; i < slotCount; i++)
            {
                if (!ObtainAllSaveGames().ContainsKey(i))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
