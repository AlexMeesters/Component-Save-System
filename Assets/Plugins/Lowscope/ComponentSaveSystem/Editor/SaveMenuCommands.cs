using UnityEngine;
using UnityEditor;
using System.IO;
using Lowscope.Saving.Data;

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
    }
}