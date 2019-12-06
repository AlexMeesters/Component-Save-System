using UnityEngine;
using UnityEditor;
using System.IO;
using Lowscope.Saving.Data;
using UnityEditor.SceneManagement;
using Lowscope.Saving.Components;

namespace Lowscope.SaveMaster.EditorTools
{
    public class SaveMenuCommands
    {
#if UNITY_EDITOR

        [UnityEditor.MenuItem(itemName: "Saving/Open Save Location")]
        public static void OpenSaveLocation()
        {
#if UNITY_EDITOR_WIN
            string dataPath = string.Format("{0}/{1}", Application.persistentDataPath, SaveSettings.Get().fileFolderName);

            Directory.CreateDirectory(dataPath);

            string path = dataPath.Replace(@"/", @"\");   // explorer doesn't like front slashes
            System.Diagnostics.Process.Start("explorer.exe", "/open," + path);

#elif UNITY_EDITOR_OSX

        string macPath = path.Replace("\\", "/"); // mac finder doesn't like backward slashes
        bool openInsidesOfFolder = false;

		if ( System.IO.Directory.Exists(macPath) ) // if path requested is a folder, automatically open insides of that folder
		{
			openInsidesOfFolder = true;
		}
 
		if ( !macPath.StartsWith("\"") )
		{
			macPath = "\"" + macPath;
		}
 
		if ( !macPath.EndsWith("\"") )
		{
			macPath = macPath + "\"";
		}

        string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;
        System.Diagnostics.Process.Start("open", arguments);
#endif
        }

#endif

        [MenuItem("Saving/Open Save Settings")]
        public static void OpenSaveSystemSettings()
        {
            Selection.activeInstanceID = SaveSettings.Get().GetInstanceID();
        }

        [MenuItem("Saving/Utility/Wipe Save Identifications (Active Scene)")]
        public static void WipeSceneSaveIdentifications()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            int rootObjectCount = rootObjects.Length;

            // Get all Saveables, including children and inactive.
            for (int i = 0; i < rootObjectCount; i++)
            {
                foreach (Saveable item in rootObjects[i].GetComponentsInChildren<Saveable>(true))
                {
                    item.saveIdentification = "";
                    item.OnValidate();
                }
            }
        }

        [MenuItem("Saving/Utility/Wipe Save Identifications (Active Selection(s))")]
        public static void WipeActiveSaveIdentifications()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                foreach (Saveable item in obj.GetComponentsInChildren<Saveable>(true))
                {
                    item.saveIdentification = "";
                    item.OnValidate();
                }
            }
        }
    }
}