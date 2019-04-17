using Lowscope.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace Lowscope.Saving.Extras
{
    public class SaveLoadMenu : MonoBehaviour
    {
        [SerializeField] private Button exitButton;
        [SerializeField] private Button[] slotButtons;

        private void Start()
        {
            exitButton.onClick.AddListener(CloseWindow);

            for (int i = 0; i < slotButtons.Length; i++)
            {
                Button removeSave = slotButtons[i].transform.GetChild(1).GetComponent<Button>();

                UpdateSlotButton(slotButtons[i], removeSave, i);

                int index = i;

                slotButtons[i].onClick.AddListener(() => SaveMaster.LoadSlot(index));

                if (SaveMaster.IsSlotUsed(index))
                {
                    removeSave.onClick.AddListener(() => SaveMaster.DeleteSave(index));
                    removeSave.onClick.AddListener(() => UpdateSlotButton(slotButtons[index], removeSave, index));
                }
            }
        }

        private void UpdateSlotButton(Button startButton, Button removeSaveButton , int slot)
        {
            Text slotText = startButton.GetComponentInChildren<Text>();

            bool isActiveSlot = SaveMaster.GetActiveSlot() == slot;
            bool isSlotUsed = SaveMaster.IsSlotUsed(slot);

            slotText.text = (isSlotUsed) ? GetSlotInfo(slot) : string.Format("Create new game for slot {0}", slot);

            startButton.interactable = !isActiveSlot;
            removeSaveButton.interactable = !isActiveSlot;

            removeSaveButton.gameObject.SetActive(isSlotUsed);
        }

        private string GetSlotInfo(int i)
        {
            var creationTime = SaveMaster.GetSaveCreationTime(i);
            var timePlayed = SaveMaster.GetSaveTimePlayed(i);

            return string.Format("<b>Slot {0}</b> \n Created: {1}. \n Played Time: {2}", i , creationTime, timePlayed);
        }

        private void CloseWindow()
        {
            GameObject.Destroy(this.gameObject);
        }
    }
}
