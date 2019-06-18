using Lowscope.Saving;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Lowscope.Saving.Extras
{
    public class SaveLoadMenu : MonoBehaviour
    {
        [SerializeField] private Button exitButton;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Text notificationText;

        private void Start()
        {
            exitButton.onClick.AddListener(CloseWindow);

            for (int i = 0; i < slotButtons.Length; i++)
            {
                Button removeSave = slotButtons[i].transform.GetChild(1).GetComponent<Button>();

                UpdateSlotButton(slotButtons[i], removeSave, i);

                int index = i;

                slotButtons[i].onClick.AddListener(() => LoadSlot(index));

                if (SaveMaster.IsSlotUsed(index))
                {
                    removeSave.onClick.AddListener(() => SaveMaster.DeleteSave(index));
                    removeSave.onClick.AddListener(() => UpdateSlotButton(slotButtons[index], removeSave, index));
                }
            }
        }

        private void UpdateSlotButtons()
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                Button removeSave = slotButtons[i].transform.GetChild(1).GetComponent<Button>();
                UpdateSlotButton(slotButtons[i], removeSave, i);
            }
        }

        private void UpdateSlotButton(Button startButton, Button removeSaveButton, int slot)
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

            return string.Format("<b>Slot {0}</b> \n Created: {1}. \n Played Time: {2}", i, creationTime, timePlayed);
        }

        private void CloseWindow()
        {
            GameObject.Destroy(this.gameObject);
        }

        private void LoadSlot(int index)
        {
            if (!SaveMaster.LoadSlot(index))
            {
                StopAllCoroutines();
                StartCoroutine(DisplayNotification("Failed to load slot. Check unity log."));
            }
            else
            {
                UpdateSlotButtons();
                this.gameObject.SetActive(false);
            }
        }

        private IEnumerator DisplayNotification(string text)
        {
            notificationText.text = text;
            notificationText.color = Color.white;
            notificationText.gameObject.SetActive(true);

            float t = 0;
            while (t < 2)
            {
                t += Time.deltaTime;
                notificationText.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), t / 2);
                yield return null;
            }

            notificationText.gameObject.SetActive(false);
        }
    }
}
