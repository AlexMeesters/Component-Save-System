using System.Collections.Generic;
using UnityEngine;

// Have to set execution order earlier then the save system. (-9000)
namespace Lowscope.Saving.Demo.Character
{
    [DefaultExecutionOrder(-9025)]
    public class SaveDemoResources : MonoBehaviour
    {
        /// Disclaimer: This isn't best practice in terms of handling resources
        /// This class is just for demo purposes, for very small games this (Or Resources folder) can work.
        /// Normally you would want to use Asset Bundles and save the string that points toward a given bundle.

        private static SaveDemoResources instance;

        private void Awake()
        {
            if (instance != null)
            {
                GameObject.Destroy(this.gameObject);
                Debug.LogWarning("SaveDemoResources: Duplicate singleton initialization happend.");
                return;
            }
            instance = this;
        }

        [Header("Resources")]
        [SerializeField]
        private List<Sprite> characters;

        public static int GetCharacterCount()
        {
            return instance.characters.Count;
        }

        public static Sprite GetCharacterSprite(int index)
        {
            if (index == 0 || index < instance.characters.Count)
            {
                return instance.characters[index];
            }
            else
            {
                return null;
            }
        }
    }
}
