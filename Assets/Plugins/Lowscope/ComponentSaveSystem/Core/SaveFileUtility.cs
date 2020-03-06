using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lowscope.Saving.Data;

#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

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

        private static string fileExtentionName { get { return SaveSettings.Get().fileExtensionName; } }
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

        private static void Log(string text)
        {
            if (debugMode)
            {
                Debug.Log(text);
            }
        }

        private static Dictionary<int, string> cachedSavePaths;

        public static Dictionary<int, string> ObtainSavePaths()
        {
            if (cachedSavePaths != null)
            {
                return cachedSavePaths;
            }

            Dictionary<int, string> newSavePaths = new Dictionary<int, string>();

            // Create a directory if it doesn't exist yet
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            string[] filePaths = Directory.GetFiles(DataPath);

            string[] savePaths = filePaths.Where(path => path.EndsWith(fileExtentionName)).ToArray();

            int pathCount = savePaths.Length;

            for (int i = 0; i < pathCount; i++)
            {
                Log(string.Format("Found save file at: {0}", savePaths[i]));

                int getSlotNumber;

                string fileName = savePaths[i].Substring(DataPath.Length + gameFileName.Length + 1);

                if (int.TryParse(fileName.Substring(0, fileName.LastIndexOf(".")), out getSlotNumber))
                {
                    newSavePaths.Add(getSlotNumber, savePaths[i]);
                }
            }

            cachedSavePaths = newSavePaths;

            return newSavePaths;
        }

        private static SaveGame LoadSaveFromPath(string savePath)
        {
            string data = "";

            using (var reader = new BinaryReader(File.Open(savePath, FileMode.Open)))
            {
                data = reader.ReadString();
            }

            if (string.IsNullOrEmpty(data))
            {
                Log(string.Format("Save file empty: {0}. It will be automatically removed", savePath));
                File.Delete(savePath);
                return null;
            }

            SaveGame getSave = JsonUtility.FromJson<SaveGame>(data);

            if (getSave != null)
            {
                getSave.OnLoad();
                return getSave;
            }
            else
            {
                Log(string.Format("Save file corrupted: {0}", savePath));
                return null;
            }
        }

        public static int[] GetUsedSlots()
        {
            int[] saves = new int[ObtainSavePaths().Count];

            int counter = 0;

            foreach (int item in ObtainSavePaths().Keys)
            {
                saves[counter] = item;
                counter++;
            }

            return saves;
        }

        public static int GetSaveSlotCount()
        {
            return ObtainSavePaths().Count;
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

            string savePath = "";

            if (SaveFileUtility.ObtainSavePaths().TryGetValue(slot, out savePath))
            {
                SaveGame saveGame = LoadSaveFromPath(savePath);

                if (saveGame == null)
                {
                    cachedSavePaths.Remove(slot);
                    return null;
                }

                Log(string.Format("Succesful load at slot (from cache): {0}", slot));
                return saveGame;
            }
            else
            {
                if (!createIfEmpty)
                {
                    Log(string.Format("Could not load game at slot {0}", slot));
                }
                else
                {

                    Log(string.Format("Creating save at slot {0}", slot));

                    SaveGame saveGame = new SaveGame();

                    WriteSave(saveGame, slot);

                    return saveGame;
                }

                return null;
            }
        }

        public static void WriteSave(SaveGame saveGame, int saveSlot)
        {
            string savePath = string.Format("{0}/{1}{2}{3}", DataPath, gameFileName, saveSlot.ToString(), fileExtentionName);

            if (!cachedSavePaths.ContainsKey(saveSlot))
            {
                cachedSavePaths.Add(saveSlot, savePath);
            }

            Log(string.Format("Saving game slot {0} to : {1}", saveSlot.ToString(), savePath));

            saveGame.OnWrite();

            using (var writer = new BinaryWriter(File.Open(savePath, FileMode.Create)))
            {
                var jsonString = JsonUtility.ToJson(saveGame, SaveSettings.Get().useJsonPrettyPrint);

                writer.Write(jsonString);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif
        }

        public static void DeleteSave(int slot)
        {
            string filePath = string.Format("{0}/{1}{2}{3}", DataPath, gameFileName, slot, fileExtentionName);

            if (File.Exists(filePath))
            {
                Log(string.Format("Succesfully removed file at {0}", filePath));
                File.Delete(filePath);

                if (cachedSavePaths.ContainsKey(slot))
                {
                    cachedSavePaths.Remove(slot);
                }
            }
            else
            {
                Log(string.Format("Failed to remove file at {0}", filePath));
            }

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif
        }

        public static bool IsSlotUsed(int index)
        {
            return ObtainSavePaths().ContainsKey(index);
        }

        public static int GetAvailableSaveSlot()
        {
            int slotCount = SaveSettings.Get().maxSaveSlotCount;

            for (int i = 0; i < slotCount; i++)
            {
                if (!ObtainSavePaths().ContainsKey(i))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}