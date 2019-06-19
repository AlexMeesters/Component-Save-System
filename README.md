# Unity Component Save System
Save system that is developed to co-exist with the current component system of Unity.

## Another save system for Unity?
How this solution differs from others is that you only have to write a save implementation per component (script).
This comes with the benefit that each object that has a component called "Saveable" will be saved uniquely.

You could duplicate 50 objects that use the same components that implement ISaveable. And all these objects would still get saved individually. Since a Saveable Component has a global unique identifier, and duplicates are not allowed, so a new ID gets generated.
This is useful in case you want to be able to easily save the state of multiple NPCS.

## How it looks in the editor
![Image of the saveable component](https://github.com/AlexMeesters/ComponentSaveSystem/blob/master/Images/Component-Clean.PNG)


## How does it work?

```csharp
using Lowscope.Saving;
using UnityEngine;

public class ExampleScript : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class Stats
    {
        public string Name;
        public int Experience;
        public int Health;
    }

    private Stats stats;

    // Gets synced from the SaveMaster
    public void OnLoad(string data)
    {
        stats = JsonUtility.FromJson<Stats>(data);
    }

    // Send data to the Saveable component, then into the SaveGame (On request of the save master)
    // On autosave or when SaveMaster.WriteActiveSaveToDisk() is called
    public string OnSave()
    {
        return JsonUtility.ToJson(stats);
    }

    // In case we don't want to do the save process.
    // We can decide within the script if it is dirty or not, for performance.
    public bool OnSaveCondition()
    {
        return true;
    }
}
```
