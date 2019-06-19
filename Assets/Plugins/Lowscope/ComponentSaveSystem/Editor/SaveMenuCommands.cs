using UnityEngine;
using UnityEditor;
using System.IO;
using Lowscope.Saving.Data;

namespace Lowscope.SaveMaster.EditorTools
{
    public class SaveMenuCommands
    {
        [MenuItem("Saving/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            string dataPath = string.Format("{0}/{1}", Application.persistentDataPath, SaveSettings.Get().fileFolderName);

            Directory.CreateDirectory(dataPath);

            string path = dataPath.Replace(@"/", @"\");   // explorer doesn't like front slashes
            System.Diagnostics.Process.Start("explorer.exe", "/open," + path);
        }

        [MenuItem("Saving/Open Save Settings")]
        public static void OpenSaveSystemSettings()
        {
            Selection.activeInstanceID = SaveSettings.Get().GetInstanceID();
        }
    }
}