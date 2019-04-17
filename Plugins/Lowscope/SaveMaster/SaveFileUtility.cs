using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lowscope.Saving.Data;

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

        string[] savePaths = filePaths.Where(path => path.EndsWith(fileExtentionName)).ToArray();

        Dictionary<int, SaveGame> gameSaves = new Dictionary<int, SaveGame>();

        for (int i = 0; i < savePaths.Length; i++)
        {
            Log(string.Format("Found save at: {0}", savePaths[i]));

            using (var reader = new BinaryReader(File.Open(savePaths[i], FileMode.Open)))
            {
                string dataString = reader.ReadString();

                if (!string.IsNullOrEmpty(dataString))
                {
                    LoadSaveFromPath(savePaths[i], ref gameSaves, dataString);
                }
                else
                {
                    Log(string.Format("Save file empty: {0}", savePaths[i]));
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
            Log(string.Format("Save file corrupted: {0}", savePath));
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
            Log(string.Format("Succesful load at slot (from cache): {0}", slot));
            return getSave;
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
        if (cachedSaveGames == null)
        {
            cachedSaveGames = new Dictionary<int, SaveGame>();
        }

        if (!cachedSaveGames.ContainsKey(saveSlot))
        {
            cachedSaveGames.Add(saveSlot, saveGame);
        }

        string savePath = string.Format("{0}/{1}{2}{3}", DataPath, gameFileName, saveSlot.ToString(), fileExtentionName);

        Log(string.Format("Saving game slot {0} to : {1}", saveSlot.ToString(), savePath));

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
        string filePath = string.Format("{0}/{1}{2}{3}", DataPath, gameFileName, slot, fileExtentionName);

        if (File.Exists(filePath))
        {
            Log(string.Format("Succesfully removed file at {0}", filePath));
            File.Delete(filePath);

            if (cachedSaveGames.ContainsKey(slot))
            {
                cachedSaveGames.Remove(slot);
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
