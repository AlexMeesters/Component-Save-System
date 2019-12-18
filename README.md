
<p align="center">
  <img src="https://github.com/AlexMeesters/Component-Save-System/blob/master/Images/ComponentSaveSystem.png">
</p>

# Unity Component Save System
A free save system that is developed to co-exist with the current component system of Unity.

[NOW ALSO AVAILABLE ON THE UNITY ASSET STORE!](https://assetstore.unity.com/packages/tools/utilities/component-save-system-159191?aid=1101lHUQ)

Introduction video of how to use the plugin.

[![IMAGE ALT TEXT](https://img.youtube.com/vi/2jfLTmDKTg8/2.jpg)](https://www.youtube.com/watch?v=2jfLTmDKTg8)

## Another save system for Unity?
How this solution differs from others is that you only have to write a save implementation per component (script).
This comes with the benefit that each object that has a component called "Saveable" will be saved uniquely.

You could duplicate 50 objects that use the same components that implement ISaveable. And all these objects would still get saved individually. Since a Saveable Component has a global unique identifier, and duplicates are not allowed, so a new ID gets generated.
This is useful in case you want to be able to easily save the state of multiple NPCS.

## How does it work?

![Save architectue](https://github.com/AlexMeesters/ComponentSaveSystem/blob/master/Images/savearchitecture.jpg)

You can find more about the image above in [this blogpost](https://low-scope.com/unity-plugin-free-save-system).

## How does it work in practice?

You add a component called "Saveable" to the root of a GameObject which you want to save.
This is a component that fetches all components that implement ISaveable. The saveable component responds to sync requests sent by the SaveMaster. 

Saving to the SaveGame is also done by the Saveable when it gets destroyed, however nothing gets written to disk until the SaveMaster decides to actually save. The benefit of having destroyed objects set data on the SaveGame object is that it prevents objects from being excluded during a save action. So for instance when you exit a room, everything gets set but not written yet to file. This is great if you want to have specific "Save Points" and you don't want to think about how the objects in the other room get saved or loaded.

![AddedSampleComponents](https://github.com/AlexMeesters/ComponentSaveSystem/blob/master/Images/Component-AddedSampleComponents.PNG)

The image above is with [all pre-made components](https://github.com/AlexMeesters/ComponentSaveSystem/tree/master/Assets/Plugins/Lowscope/ComponentSaveSystem/Components) added that implement ISaveable.

Here is an example of how to create your own script:

```csharp
using Lowscope.Saving;
using UnityEngine;

public class TestScript : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class Stats
    {
        public string Name = "Test Name";
        public int Experience = 100;
        public int Health = 50;
    }

    [SerializeField]
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
Ô{
    "metaData": {
        "gameVersion": 0,
        "creationDate": "11/26/2019 2:47:31 PM",
        "timePlayed": "00:00:20"
    },
    "saveData": [
        {
            "guid": "TestScene-TestScriptGameobject-d4dbf-TestScript-ac11c",
            "data": "{\"Name\":\"Test Name\",\"Experience\":100,\"Health\":50}"
        }
    ]
}
```

The guid is defined as:
"(Scene Name)-(GameObject Name)-(objectGUID)-(Script Name)-(scriptGUID)"

The GUIDS are used to avoid both object and script name conflicts.

## How to use

When the plugin is imported, you can configure it by going to Saving/Open Save Settings in the top menu of Unity.

![pluginsettings](https://github.com/AlexMeesters/Component-Save-System/blob/master/Images/pluginsettings.PNG)

The most important settings during setup are:
* Auto Save - Automatically writes current SaveGame to file upon ApplicationExit/Pause
* Auto Save On Slot Switch - Automatically writes current SaveGame upon switching save slot
* Load Default slot on start - Once startup, the component will automatically load the designated slot.
This is useful if you don't plan on using any other save slots, and you just want to have your game saved.

Also take note of the hotkeys and use slot menu in the extras tab. 

In case you want full control through just C# I reccomend turning all the autosaving off.
The SaveMaster gets instantiated before any scene loads using RunTimeInitializeOnLoad()
Meaning you can directly use the system. 

### SaveMaster methods

These are all the methods of the save master. I've 
put them into categories. In case you have a very simple game, then you may never have to
actually call one of these methods. All methods with (int slot) in them also have a version that works
on the current active slot.

```csharp
// Utility
SaveMaster.DeactivatedObjectExplicitly(GameObject gameObject) -> bool

// Obtaining Saveslots
SaveMaster.GetActiveSlot() -> int
SaveMaster.HasUnusedSlots() -> bool
SaveMaster.GetUsedSlots() -> int[]
SaveMaster.IsSlotUsed(int slot) -> bool

// Setting Saveslots
SaveMaster.SetSlotToLastUsedSlot(bool notifyListeners) -> bool
SaveMaster.SetSlotToNewSlot(bool notifyListeners, out int slot) -> bool
SaveMaster.SetSlot(int slot, bool notifyListeners, SaveGame saveGame = null) -> void

// Getting Data from active save or slot
SaveMaster.GetSaveCreationTime(int slot) -> DateTime
SaveMaster.GetSaveTimePlayed(int slot) -> TimeSpan
SaveMaster.GetSaveVersion(int slot) -> int

// Writing to disk or removing (Writing happens automatically on default settings)
SaveMaster.WriteActiveSaveToDisk() -> void
SaveMaster.DeleteSave(int slot) -> void

// Syncing and adding of saveables. (No need to use these on default settings)
SaveMaster.AddListener(Saveable saveable) -> void
SaveMaster.RemoveListener(Saveable saveable) -> void
SaveMaster.SyncSave() -> void
SaveMaster.SyncLoad() -> void

// Spawning saved instances
SaveMaster.SpawnSavedPrefab(InstanceSource source, string filePath) -> GameObject

// Get data directly without a Saveable() as intermediate.
SaveMaster.GetSaveableData<T>(int slot, string saveableId, string componentId, out T data) -> bool

// Storing variables like playerprefs.
SaveMaster.SetInt(string key, int value) -> void
SaveMaster.GetInt(string key, int defaultValue = -1) -> int
SaveMaster.SetFloat(string key, float value) -> void
SaveMaster.GetFloat(string key, float defaultValue = -1) -> float
SaveMaster.SetString(string key, string value) -> void
SaveMaster.GetString(string key, string defaultValue = -1) -> string
```

## Performance tests

### The code is still getting updated from time to time, so performance may differ from this.

Keep in mind that in normal circumstances, you would not sync 4000 components at a time, unless you do it explicitly.
This is because all components that implement ISaveable get written to the SaveGame class when the GameObject gets destroyed. 
And eventually this SaveGame is written to Disk upon game exit/pause, slot switch or savepoint. This depends on the plugin configuration you choose to have.

Each object contains 4 saveable components:
* Save Positionn (x,y,z)
* Save Rotation (x,y,z)
* Save Scale (x,y,z)
* Save Visibility (false,true)

Tests have been done in a mono build.

### Test with 1000 unique objects - I7 8700K and SSD (4000 components, randomized positions, scale and rotations)

- Sync Save: ~0.70 Milliseconds
- Sync Load: ~2.5 Milliseconds
- Sync & Write to disk : ~18 Milliseconds
- Sync & Load from disk: ~23 Milliseconds

### Test with 1000 unique objects - Samsung Galaxy A3 2016 (4000 components , Random)

Initial save/loads can be higher.

- Sync Save: ~9 Milliseconds
- Sync Load: ~40 Milliseconds
- Sync & Write to disk : ~229 Milliseconds
- Sync & Load from disk: ~385 Milliseconds

## What is still missing?

* Metadata files for the saves, making it less expensive to read
basic data like creation date and time played.
* Encryption of save files
* Data corruption prevention measures
* Tracking of edit history, in order to remove bloat from existing
save files. (May be overengineering tough)
* Potentially more fixes & features that are needed eventually.




## Credits & Licence

The plugin is MIT licenced. Read the LICENCE file.

Made by me, Alex Meesters. I'm currently using it in product that I create under the company name "Lowscope".
I've chosen to release it under the MIT licence. Which means you can use it freely in a commerical project with the condition of adding me in the credits.

This system was primarily made and designed for the [RPG Farming Kit](https://assetstore.unity.com/packages/templates/packs/rpg-farming-kit-121080?aid=1101lHUQ). After a lot of iterations it became more generalized since I also needed a save system in other projects.
All the code & design of it was done by me. Any constructive criticism/feedback on it is appreciated!

In case you want to support me, please consider buying [Health Pro](https://assetstore.unity.com/packages/tools/utilities/health-pro-effects-132006?aid=1101lHUQ) or [RPG Farming Kit](https://assetstore.unity.com/packages/templates/packs/rpg-farming-kit-121080?aid=1101lHUQ). 

[You can also support me through the Github Sponsors here!](https://github.com/sponsors/AlexMeesters)
