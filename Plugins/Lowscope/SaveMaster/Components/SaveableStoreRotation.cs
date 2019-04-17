using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    [AddComponentMenu("Saving/Components/Store Rotation"), DisallowMultipleComponent]
    public class SaveableStoreRotation : MonoBehaviour, ISaveable
    {
        private Vector3 lastRotation;

        [System.Serializable]
        public struct SaveData
        {
            public Vector3 rotation;
        }

        public void OnLoad(string data)
        {
            lastRotation = JsonUtility.FromJson<SaveData>(data).rotation;
            this.transform.rotation = Quaternion.Euler(lastRotation);
        }

        public string OnSave()
        {
            lastRotation = this.transform.rotation.eulerAngles;
            return JsonUtility.ToJson(new SaveData() { rotation = this.transform.rotation.eulerAngles });
        }

        public bool OnSaveCondition()
        {
            return lastRotation != this.transform.rotation.eulerAngles;
        }
    }
}
