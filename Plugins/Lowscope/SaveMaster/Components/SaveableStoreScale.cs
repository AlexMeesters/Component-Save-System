using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    [AddComponentMenu("Saving/Components/Store Position"), DisallowMultipleComponent]
    public class SaveableStoreScale : MonoBehaviour, ISaveable
    {
        private Vector3 lastScale;

        [System.Serializable]
        public struct SaveData
        {
            public Vector3 scale;
        }

        public void OnLoad(string data)
        {
            this.transform.localScale = JsonUtility.FromJson<SaveData>(data).scale;
            lastScale = this.transform.localScale;
        }

        public string OnSave()
        {
            lastScale = this.transform.localScale;
            return JsonUtility.ToJson(new SaveData() { scale = this.transform.localScale });
        }

        public bool OnSaveCondition()
        {
            return lastScale != this.transform.localScale;
        }
    }
}
