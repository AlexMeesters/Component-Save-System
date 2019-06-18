using UnityEngine;
using System.Collections;
using Lowscope.Saving.Components;
using Lowscope.Saving;

/// <summary>
/// Saved instances are objects that should respawn when they are not destroyed.
/// </summary>
public class SavedInstance : MonoBehaviour
{
    private SaveInstanceManager instanceManager;
    private Saveable saveable;
 
    public void Configure(Saveable saveable, SaveInstanceManager instanceManager)
    {
        this.saveable = saveable;
        this.instanceManager = instanceManager;
    }

    private void OnDestroy()
    {
        if (SaveMaster.DeactivatedObjectExplicitly(this.gameObject))
        {
            SaveMaster.WipeData(saveable);
            instanceManager.DestroyObject(saveable);
        }
    }
}
