using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    /// <summary>
    /// Example class of how to store a rotation.
    /// Also very useful for people looking for a simple way to store a rotation.
    /// </summary>

    [AddComponentMenu("Saving/Components/Save Rotation"), DisallowMultipleComponent]
    public class SaveRotation : MonoBehaviour, ISaveable
    {
        private Vector3 lastRotation;
        private Vector3 activeRotation;

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
            lastRotation = activeRotation;
            return JsonUtility.ToJson(new SaveData() { rotation = this.transform.rotation.eulerAngles });
        }

        public bool OnSaveCondition()
        {
            activeRotation = this.transform.rotation.eulerAngles;
            return lastRotation != activeRotation;
        }
    }
}
