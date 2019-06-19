# Unity Component Save System
Save system that is developed to co-exist with the current component system of Unity.

[Simple video to see it in action](https://giant.gfycat.com/FavorableVioletEastrussiancoursinghounds.webm)

## Another save system for Unity?
How this solution differs from others is that you only have to write a save implementation per component (script).
This comes with the benefit that each object that has a component called "Saveable" will be saved uniquely.

You could duplicate 50 objects that use the same components that implement ISaveable. And all these objects would still get saved individually. Since a Saveable Component has a global unique identifier, and duplicates are not allowed, so a new ID gets generated.
This is useful in case you want to be able to easily save the state of multiple NPCS.

## How does it work?

![Save architectue](https://github.com/AlexMeesters/ComponentSaveSystem/blob/master/Images/savearchitecture.jpg)

## How does it work in practice?

You add a component called "Saveable" to the root of a GameObject which you want to save.
This is a component that fetches all components that implement ISaveable. The saveable component responds to sync requests sent by the SaveMaster.

<p align="center"> 
<img src="https://github.com/AlexMeesters/ComponentSaveSystem/blob/master/Images/Component-AddedSampleComponents.PNG">
</p>

The image above is with [all pre-made components](https://github.com/AlexMeesters/ComponentSaveSystem/tree/master/Assets/Plugins/Lowscope/ComponentSaveSystem/Components) added that implement ISaveable.

Here is an example of how to create your own script:

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

## How the save file looks like with just the ExampleScript:

```json

ß{"metaData":{"gameVersion":0,"creationDate":"6/19/2019 2:16:52 AM","timePlayed":"00:00:02","lastActiveScene":"TestScene","lastAdditiveScenes":[]},"saveData":[{"guid":"TestScene-GameObject-d5f95","data":"{\"saveStructures\":[{\"identifier\":\"ExampleScript 915ce\",\"data\":\"{\\\"Name\\\":\\\"Test\\\",\\\"Experience\\\":25,\\\"Health\\\":25}\"}]}"}]}

```

## Performance tests (I7 8700K and SSD)

Each object contains 4 saveable components:
* Save Position
* Save Rotation
* Save Scale
* Save Visibility

### Test with 1000 unique objects (Non-Random positions, scale and rotations)

- 1000 Object Sync & Write to disk : ~18 Milliseconds
- 1000 Object Sync & Load to disk: ~26 Milliseconds
- 1000 Object Sync Save: ~9 Milliseconds
- 1000 Object Sync Load: ~15 Milliseconds

Resulting filesize:
* Pretty Print: 395 KB
* Non-pretty Print: 238 KB

### Test with 1000 unique objects (Randomized positions, scale and rotations)

- 1000 Object Sync & Write to disk : ~26 Milliseconds
- 1000 Object Sync & Load to disk: ~38 Milliseconds
- 1000 Object Sync Save: ~12 Milliseconds
- 1000 Object Sync Load: ~21 Milliseconds

Resulting filesize:
* Pretty Print: 809 KB
* Non-pretty Print: 601 KB
