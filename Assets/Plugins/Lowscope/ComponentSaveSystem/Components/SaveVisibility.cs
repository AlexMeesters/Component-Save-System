using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    /// <summary>
    /// Example class of how to store the visability of an object.
    /// This one took a bit longer, because of the edge-cases with scene loading and unloading
    /// </summary>

    [AddComponentMenu("Saving/Components/Save Visibility"), DisallowMultipleComponent]
    public class SaveVisibility : MonoBehaviour, ISaveable
    {
        private bool isEnabled;

        private void OnEnable()
        {
            isEnabled = true;
        }

        private void OnDisable()
        {
            // Ensure that it doesn't get toggled when the object is
            // deactivated /activated during scene load/unload
            if (SaveMaster.DeactivatedObjectExplicitly(this.gameObject))
            {
                isEnabled = false;
            }
        }

        public void OnLoad(string data)
        {
            isEnabled = (data == "1");
            gameObject.SetActive(isEnabled);
        }

        public string OnSave()
        {
            return isEnabled ? "1" : "0";
        }

        public bool OnSaveCondition()
        {
            return true;
        }
    }
}
