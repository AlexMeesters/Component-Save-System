using System;
using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    [AddComponentMenu("Saving/Components/Store Position"), DisallowMultipleComponent]
    public class SaveableStorePosition : MonoBehaviour, ISaveable
    {
        Vector3 lastPosition;

        [Serializable]
        public struct SaveData
        {
            public Vector3 position;
        }

        public void OnLoad(string data)
        {
            var pos = JsonUtility.FromJson<SaveData>(data).position;
            transform.position = pos;
            lastPosition = pos;
        }

        public string OnSave()
        {
            lastPosition = transform.position;
            return JsonUtility.ToJson(new SaveData { position = lastPosition });
        }

        public bool OnSaveCondition()
        {
            return lastPosition != transform.position;
        }
    }
}
