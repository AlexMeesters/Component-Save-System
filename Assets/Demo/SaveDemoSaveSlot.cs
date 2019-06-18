using Lowscope.Saving.Demo.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Lowscope.Saving.Demo
{
    public class SaveDemoSaveSlot : MonoBehaviour
    {
        /// <summary>
        /// This is a very specific example of how you can retrieve data from a specific saveable component
        /// This is only really neccicary if you want to display data in the main menu.
        /// 
        /// An alternative way would be to make a copy of the character with disabled movement and display that in the main menu.
        /// While having manual save/load toggled on, meaning you can prevent the saveable from sending data to the savegame.
        /// </summary>

        [Header("Coniguration")]
        [SerializeField] private int slot;

        [SerializeField, Tooltip("What identification does the saveable component of the character have?")]
        private string charSaveableIdentification;

        [SerializeField, Tooltip("What identification does the character component have on the saveable?")]
        private string charComponentIdentification;

        [Header("References")]
        [SerializeField] private Text characterName;
        [SerializeField] private RawImage rawImage;
        [SerializeField] private Button button;
        [SerializeField] private Text timePlayed;

        public Button Button
        {
            get { return button; }
        }

        public int Slot
        {
            get { return slot; }
        }

        public void UpdateInfo()
        {
            SaveDemoCharacter.SaveData charData;

            var colorBlock = button.colors;
            colorBlock.normalColor = slot == SaveMaster.GetActiveSlot() ? new Color(0.9f, 0.9f, 0.9f) : Color.white;
            colorBlock.highlightedColor = colorBlock.normalColor;
            button.colors = colorBlock;

            // This method call helps simplify the process of obtaining specific data from a SaveGame
            bool foundData = SaveMaster.GetSaveableData<SaveDemoCharacter.SaveData>(slot, charSaveableIdentification, charComponentIdentification, out charData);

            characterName.text = (!foundData) ? "Empty" : charData.characterDisplayName;
            rawImage.texture = (!foundData) ? null : SaveDemoResources.GetCharacterSprite(charData.characterIndex).texture;
            rawImage.gameObject.SetActive(foundData);

            timePlayed.text = SaveMaster.GetSaveTimePlayed(slot).ToString();
        }
    }
}
