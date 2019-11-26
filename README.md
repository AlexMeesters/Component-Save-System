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

Saving to the SaveGame is also done by the Saveable when it gets destroyed, however nothing gets written to disk until the SaveMaster decides to actually save. The benefit of having destroyed objects set data on the SaveGame object is that it prevents objects from being excluded during a save action. So for instance when you exit a room, everything gets set but not written yet to file. This is great if you want to have specific "Save Points" and you don't want to think about how the objects in the other room get saved or loaded.

![AddedSampleComponents](https://github.com/AlexMeesters/ComponentSaveSystem/blob/master/Images/Component-AddedSampleComponents.PNG)

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

### Most used SaveMaster methods
```csharp
// Returns the active slot. -1 means no slot is loaded
SaveMaster.GetActiveSlot() 

// Tries to set the current slot to the last used one.
SaveMaster.SetSlotToLastUsedSlot(bool syncListeners) 

// Tries to load the last used slot
SaveMaster.LoadLastUsedSlot()

// Attempts to set the slot to the first unused slot. Useful for creating a new game.
SaveMaster.SetSlotToFirstUnused(bool syncListeners, out int slot)

// Will load the last used scene for save game, and set the slot. 
// Current scene also gets saved, if any slot is currently set. (And if AutoSaveOnSlotSwitch is on)
// If slot is empty, it will still set it, and load the default set starting scene.
SaveMaster.LoadSlot(int slot, string defaultScene = "")

// Set the active save slot
SaveMaster.SetSlot(int slot, bool syncListeners)

// Attempts to get a SaveGame, purely for the data.
SaveMaster.GetSave(int slot, bool createIfEmpty = true)

//Removes the active save file. Based on the save slot index.
SaveMaster.DeleteActiveSaveGame()

// Sends notification to all subscribed Saveables to save to the SaveGame
Savemaster.SyncSave()

// Sends notification to all subscribed Saveables to load from the SaveGame
Savemaster.LoadSave()
```

## Performance tests

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

## Credits


This system was primarily made and designed for the [RPG Farming Kit](https://assetstore.unity.com/packages/templates/packs/rpg-farming-kit-121080?aid=1101lHUQ). After a lot of iterations it became more generalized since I also needed a save system in other projects.
All the code & design of it was done by me. Any constructive criticism/feedback on it is appreciated!

In case you want to support me, please concider buying [Health Pro](https://assetstore.unity.com/packages/tools/utilities/health-pro-effects-132006?aid=1101lHUQ) or [RPG Farming Kit](https://assetstore.unity.com/packages/templates/packs/rpg-farming-kit-121080?aid=1101lHUQ). Or you can support me on my desolated [Patreon](https://www.patreon.com/lowscope)
