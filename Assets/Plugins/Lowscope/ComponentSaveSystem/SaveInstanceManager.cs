using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Lowscope.Saving;
using Lowscope.Saving.Components;
using Lowscope.Saving.Enums;

/// <summary>
/// Each scene has a Save Instance Manager
/// The responsibility for this manager is to keep track of all saved instances within that scene.
/// Examples of saved instances are keys or items you have dropped out of your inventory.
/// </summary>
[DefaultExecutionOrder(-9100)]
public class SaveInstanceManager : MonoBehaviour, ISaveable
{
    private Dictionary<GameObject, SpawnInfo> spawnInfo = new Dictionary<GameObject, SpawnInfo>();
    private HashSet<string> loadedIDs = new HashSet<string>();

    private bool isDirty;

    [System.Serializable]
    public class SaveData
    {
        public SpawnInfo[] infoCollection;
    }

    [System.Serializable]
    public struct SpawnInfo
    {
        public InstanceSource source;
        public string filePath;
        public string saveIdentification;
    }

    public void DestroyObject(Saveable saveable)
    {
        if (spawnInfo.ContainsKey(saveable.gameObject))
        {
            spawnInfo.Remove(saveable.gameObject);
            loadedIDs.Remove(saveable.saveIdentification);

            isDirty = true;
        }
    }

    public GameObject SpawnObject(InstanceSource source, string filePath, string saveIdentification = "")
    {
        GameObject getResource = null;

        switch (source)
        {
            case InstanceSource.Resources:

                getResource = Resources.Load(filePath) as GameObject;

                break;
            default:
                break;
        }

        if (getResource == null)
        {
            Debug.LogWarning(string.Format("Invalid resource path: {0}", filePath));
            return null;
        }

        // We will temporarily set the resource to disabled. Because we don't want to enable any
        // of the components yet.
        bool resourceState = getResource.gameObject.activeSelf;
        getResource.gameObject.SetActive(false);

        GameObject instance = GameObject.Instantiate(getResource, getResource.transform.position, getResource.transform.rotation);

        // After instantiating we reset the resource back to it's original state.
        getResource.gameObject.SetActive(resourceState);

        Saveable saveable = instance.GetComponent<Saveable>();

        if (saveable == null)
        {
            Debug.LogWarning("Save Instance Manager: No saveable added to spawned object." +
                " Scanning for ISaveables during runtime is more costly.");
            saveable = instance.AddComponent<Saveable>();
            saveable.ScanAddSaveableComponents();
        }

        SavedInstance savedInstance = instance.AddComponent<SavedInstance>();
        savedInstance.Configure(saveable, this);

        // In case the object has no idenfication, which applies to all prefabs.
        // Then we give it a new identification, and we store it into our spawninfo array so we know to spawn it again.
        if (string.IsNullOrEmpty(saveIdentification))
        {
            saveable.saveIdentification = string.Format("{0}-{2}-{1}", this.gameObject.scene.name, saveable.name, spawnInfo.Count);

            spawnInfo.Add(instance, new SpawnInfo()
            {
                filePath = filePath,
                saveIdentification = saveable.saveIdentification,
                source = source
            });

            loadedIDs.Add(saveable.saveIdentification);
        }
        else
        {
            saveable.saveIdentification = saveIdentification;
            loadedIDs.Add(saveable.saveIdentification);
        }

        // This action has done something to make it eligible for saving
        isDirty = true;

        instance.gameObject.SetActive(true);

        return instance;
    }

    public string OnSave()
    {
        isDirty = false;

        int c = spawnInfo.Count;

        SaveData data = new SaveData()
        {
            infoCollection = new SpawnInfo[c]
        };

        int i = 0;
        foreach (SpawnInfo item in spawnInfo.Values)
        {
            data.infoCollection[i] = item;
            i++;
        }

        return JsonUtility.ToJson(data);
    }

    public void OnLoad(string data)
    {
        //if (isLoaded)
        //{
        //    Debug.LogWarning("Attempted to load instance manager twice");
        //    return;
        //}

        //spawnInfo.Clear();

        SaveData saveData = JsonUtility.FromJson<SaveData>(data);

        if (saveData != null && saveData.infoCollection != null)
        {
            int itemCount = saveData.infoCollection.Length;

            for (int i = 0; i < itemCount; i++)
            {
                if (loadedIDs.Contains(saveData.infoCollection[i].saveIdentification))
                {
                    return;
                }

                var source = saveData.infoCollection[i].source;
                var path = saveData.infoCollection[i].filePath;
                var id = saveData.infoCollection[i].saveIdentification;

                var obj = SpawnObject(source, path, id);

                spawnInfo.Add(obj, saveData.infoCollection[i]);
            }
        }
    }

    public bool OnSaveCondition()
    {
        return isDirty;
    }
}
