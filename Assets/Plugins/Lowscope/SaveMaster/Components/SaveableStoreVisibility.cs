using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Components
{
    [AddComponentMenu("Saving/Components/Store Visibility"), DisallowMultipleComponent]
    public class SaveableStoreVisibility : MonoBehaviour, ISaveable
    {
        private bool isEnabled;

        private void OnEnable()
        {
            isEnabled = true;
        }

        private void OnDisable()
        {
            if (SaveMaster.DeactivatedObjectExplicitly(this.gameObject))
            {
                isEnabled = false;
            }
        }

        public void OnLoad(string data)
        {
            isEnabled = data == "1";
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
