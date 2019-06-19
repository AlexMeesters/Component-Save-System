
using Lowscope.Saving.Data;
using UnityEditor;

namespace Lowscope.SaveMaster.EditorTools
{
    [InitializeOnLoad]
    public class SaveMasterStartPopup
    {
        static SaveMasterStartPopup()
        {
            // Verifies the save settings file.
            if (SaveSettings.CreateFile())
            {
                Selection.activeInstanceID = SaveSettings.Get().GetInstanceID();
            }
        }
    }
}
