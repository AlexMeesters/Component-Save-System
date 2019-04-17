
using Lowscope.Saving.Data;
using UnityEditor;

namespace Lowscope.SaveMaster.Editor
{
    [InitializeOnLoad]
    public class SaveMasterStartPopup
    {
        static SaveMasterStartPopup()
        {
            // Verifies the save settings file.
            if (SaveSettings.TryCreateSaveSettings())
            {
                Selection.activeInstanceID = SaveSettings.Get().GetInstanceID();
            }
        }
    }
}
