using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lowscope.Saving.Data
{
    public class SaveSettings : ScriptableObject
    {
        private static SaveSettings instance;

        private void OnDestroy()
        {
            instance = null;
        }

        public static SaveSettings Get()
        {
            if (instance != null)
            {
                return instance;
            }

            var savePluginSettings = Resources.Load("Save Plugin Settings", typeof(SaveSettings)) as SaveSettings;

#if UNITY_EDITOR
            // In case the settings are not found, we create one
            if (savePluginSettings == null)
            {
                return CreateFile();
            }
#endif

            // In case it still doesn't exist, somehow it got removed.
            // We send a default instance of SavePluginSettings.
            if (savePluginSettings == null)
            {
                Debug.LogWarning("Could not find SavePluginsSettings in resource folder, did you remove it? Using default settings.");
                savePluginSettings = ScriptableObject.CreateInstance<SaveSettings>();
            }

            instance = savePluginSettings;

            return instance;
        }

#if UNITY_EDITOR

        public static SaveSettings CreateFile()
        {
            string resourceFolderPath = string.Format("{0}/{1}", Application.dataPath, "Resources");
            string filePath = string.Format("{0}/{1}", resourceFolderPath, "Save Plugin Settings.asset");

            // In case the directory doesn't exist, we create a new one.
            if (!Directory.Exists(resourceFolderPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Check if the settings file exists in the resources path
            // If not, we create a new one.
            if (!File.Exists(filePath))
            {
                instance = ScriptableObject.CreateInstance<SaveSettings>();
                UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/Resources/Save Plugin Settings.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();

                return instance;
            }
            else
            {
                return Resources.Load("Save Plugin Settings", typeof(SaveSettings)) as SaveSettings;
            }
        }

        private void OnValidate()
        {
            this.fileExtensionName = ValidateString(fileExtensionName, ".savegame", false);
            this.fileFolderName = ValidateString(fileFolderName, "SaveData", true);
            this.fileName = ValidateString(fileName, "Slot", true);

            if (fileExtensionName[0] != '.')
            {
                Debug.LogWarning("SaveSettings: File extension name needs to start with a .");
                fileExtensionName = string.Format(".{0}", fileExtensionName);
            }
        }

        private string ValidateString(string input, string defaultString, bool allowWhiteSpace)
        {
            if (string.IsNullOrEmpty(input) || (!allowWhiteSpace && input.Any(Char.IsWhiteSpace)))
            {
                Debug.LogWarning(string.Format("SaveSettings: Set {0} back to {1} " +
                                               "since it was empty or has whitespace.", input, defaultString));
                return defaultString;
            }
            else
            {
                return input;
            }
        }

#endif

        [Header("Storage Settings")]
        public string fileExtensionName = ".savegame";
        public string fileFolderName = "SaveData";
        public string fileName = "Slot";
        public bool useJsonPrettyPrint = true;

        [Header("Configuration")]
        [Range(1, 300)]
        public int maxSaveSlotCount = 300;
        [Tooltip("The save system will increment the time played since load")]
        public bool trackTimePlayed = true;
        [Tooltip("When you disable this, writing the game only happens when you call SaveMaster.Save()")]
        public bool autoSaveOnExit = true;
        [Tooltip("Should the game get saved when switching between game saves?")]
        public bool autoSaveOnSlotSwitch = true;

        [Header("Saveable")]
        [Tooltip("Will do a check if object has already been instantiated with the ID")]
        public bool resetSaveableIdOnDuplicate = true;
        [Tooltip("Will do a check if object is serialized under a different scene name")]
        public bool resetSaveableIdOnNewScene = false;
        [Tooltip("Default generated guid length for a game object")]
        [Range(5,36)]
        public int gameObjectGuidLength = 5;
        [Tooltip("Default generated guid length for a component")]
        [Range(5, 36)]
        public int componentGuidLength = 5;

        [Header("Saveable Prefabs")]
        [Tooltip("Automatically remove saved instances when changing slots")]
        public bool cleanSavedPrefabsOnSlotSwitch = true;

        [Header("Initialization")]
        public bool loadDefaultSlotOnStart = true;
        [Range(0, 299)]
        public int defaultSlot = 0;

        [Header("Extras")]
        public bool useHotkeys = false;
        public KeyCode saveAndWriteToDiskKey = KeyCode.F2;
        public KeyCode syncSaveGameKey = KeyCode.F4;
        public KeyCode syncLoadGameKey = KeyCode.F5;
        public KeyCode wipeActiveSceneData = KeyCode.F6;

        [Header("Debug (Unity Editor Only)")]
        public bool showSaveFileUtilityLog = false;
    }
}