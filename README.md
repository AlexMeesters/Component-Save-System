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

## How to use

When the plugin is imported, you can configure it by going to Saving/Open Save Settings in the top menu of Unity.

<p align="center"> 
<img src="https://github.com/AlexMeesters/Component-Save-System/blob/master/Images/pluginsettings.PNG">
</p>

The most important settings during setup are:
* Auto Save - Automatically writes current SaveGame to file upon ApplicationExit/Pause
* Auto Save On Slot Switch - Automatically writes current SaveGame upon switching save slot
* Load Default slot on start - Once startup, the component will automatically load the designated slot.
This is useful if you don't plan on using any other save slots, and you just want to have your game saved.

Also take note of the hotkeys and use slot menu in the extras tab. 

In case you want full control through just C# I reccomend turning all the autosaving off.
The SaveMaster gets instantiated before any scene loads using RunTimeInitializeOnLoad()
Meaning you can directly use the system. The methods below are most important. 
It is reccomended to see what else is available.

```
// Returns the active slot. -1 means no slot is loaded
SaveMaster.GetActiveSlot() 

// Tries to set the current slot to the last used one.
SaveMaster.SetSlotToLastUsedSlot(bool syncListeners) 

// Tries to load the last used slot
SaveMaster.LoadLastUsedSlot()

// Attempts to set the slot to the first unused slot. Useful for creating a new game.
SaveMaster.SetSlotToFirstUnused(bool syncListenersout int slot)

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

## Performance tests (I7 8700K and SSD)

Keep in mind that in normal circumstances, you would not sync 4000 components at a time, unless you do it explicitly.
This is because all components that implement ISaveable get written to the SaveGame class when the GameObject gets destroyed. 
And eventually this SaveGame is written to Disk upon game exit/pause, slot switch or savepoint. This depends on the plugin configuration you choose to have.

Each object contains 4 saveable components:
* Save Position
* Save Rotation
* Save Scale
* Save Visibility

### Test with 1000 unique objects (4000 components, non-random positions, scale and rotations)

- Sync & Write to disk : ~18 Milliseconds
- Sync & Load from disk: ~26 Milliseconds
- Sync Save: ~9 Milliseconds
- Sync Load: ~15 Milliseconds

* Pretty Print: 395 KB
* Non-pretty Print: 238 KB

### Test with 1000 unique objects (4000 components, randomized positions, scale and rotations)

- Sync & Write to disk : ~26 Milliseconds
- Sync & Load from disk: ~38 Milliseconds
- Sync Save: ~12 Milliseconds
- Sync Load: ~21 Milliseconds

* Pretty Print: 809 KB
* Non-pretty Print: 601 KB

## Performance tests (Samsung Galaxy A3, MONO , Non-Random)

### Test with 1000 unique objects (4000 components, Samsung Galaxy A3, MONO , Random)

Initial save/loads can be a lot higher.

- 1000 Object Sync & Write to disk : ~130 Milliseconds
- 1000 Object Sync & Load from disk: ~167 Milliseconds
- 1000 Object Sync Save: ~71 Milliseconds
- 1000 Object Sync Load: ~100 Milliseconds


### Test with 1000 unique objects (4000 components, Samsung Galaxy A3, MONO , Non-Random)

Initial save/loads can be a lot higher.

- 1000 Object Sync & Write to disk : ~237 Milliseconds
- 1000 Object Sync & Load from disk: ~320 Milliseconds
- 1000 Object Sync Save: ~130 Milliseconds
- 1000 Object Sync Load: ~200 Milliseconds
