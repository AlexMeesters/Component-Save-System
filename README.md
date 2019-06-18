# Unity Component Save System
Save system that is developed to co-exist with the current component system of Unity.

## Another save system for Unity? There are plenty...

How this solution differs from others is that it is made to be additive.
Meaning that you can slap a Saveable component on as many GameObjects as you like. And it would still work without any extra trouble.

It is designed to be additive of nature, meaning you can slap it on any game object.
And it will save it individually, based on the randomized GUID that is generated. You can modify this to your liking.

## How it looks in the editor
![Image of the saveable component](https://github.com/AlexMeesters/ComponentSaveSystem/blob/master/Images/Component-Clean.PNG)


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
