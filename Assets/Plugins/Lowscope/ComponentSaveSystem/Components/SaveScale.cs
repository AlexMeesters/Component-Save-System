using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    /// <summary>
    /// Example class of how to store the scale of a gameObject.
    /// Also very useful for people looking for a simple way to store the scale.
    /// </summary>

    [AddComponentMenu("Saving/Components/Save Scale"), DisallowMultipleComponent]
    public class SaveScale : MonoBehaviour, ISaveable
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
