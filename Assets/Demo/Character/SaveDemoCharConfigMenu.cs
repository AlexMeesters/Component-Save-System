using System;
using System.Collections;
using System.Collections.Generic;
using Lowscope.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lowscope.Saving.Demo.Character
{
    public class SaveDemoCharConfigMenu : MonoBehaviour
    {
        [Header("External References")]
        [SerializeField] private SaveDemoCharacter character;

        [Header("User Interface References")]
        [SerializeField] private InputField characterNameInput;
        [SerializeField] private Slider characterSpriteSlider;
        [SerializeField] private Button forceLoadButton;
        [SerializeField] private Button forceSaveButton;
        [SerializeField] private Button wipeAndReloadButton;
        [SerializeField] private List<SaveDemoSaveSlot> saveSlotButtons;
        [SerializeField] private Text saveSlotInfo;

        private void Start()
        {
            characterSpriteSlider.maxValue = SaveDemoResources.GetCharacterCount() - 1;

            // Subscribe to all events
            characterSpriteSlider.onValueChanged.AddListener(OnChangeCharacterSprite);
            characterNameInput.onValueChanged.AddListener(OnChangeCharacterName);
            forceLoadButton.onClick.AddListener(OnLoad);
            forceSaveButton.onClick.AddListener(OnSave);
            wipeAndReloadButton.onClick.AddListener(WipeAndReloadSave);

            for (int i = 0; i < saveSlotButtons.Count; i++)
            {
                var index = i;

                saveSlotButtons[i].UpdateInfo();

                saveSlotButtons[i].Button.onClick.AddListener(() =>
                {
                    SaveMaster.LoadSlot(index, this.gameObject.scene.name);
                });
            }

            UpdateSlotInfo();
        }

        private void UpdateActiveSlotButtonInfo()
        {
            int saveSlot = SaveMaster.GetActiveSlot();

            for (int i = 0; i < saveSlotButtons.Count; i++)
            {
                if (saveSlot == saveSlotButtons[i].Slot)
                {
                    saveSlotButtons[i].UpdateInfo();
                    return;
                }
            }
        }

        private void UpdateSlotInfo()
        {
            var activeSlot = SaveMaster.GetActiveSlot() + 1;

            characterNameInput.text = character.CharacterDisplayName;
            characterSpriteSlider.value = character.CharacterSpriteIndex;
            saveSlotInfo.text = String.Format("Active Slot : {0}", activeSlot == -1 ? "None" : activeSlot.ToString());

            for (int i = 0; i < saveSlotButtons.Count; i++)
            {
                saveSlotButtons[i].UpdateInfo();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to all events
            characterSpriteSlider.onValueChanged.RemoveListener(OnChangeCharacterSprite);
            characterNameInput.onValueChanged.RemoveListener(OnChangeCharacterName);
            forceLoadButton.onClick.RemoveListener(OnLoad);
            forceSaveButton.onClick.RemoveListener(OnSave);
            wipeAndReloadButton.onClick.RemoveListener(WipeAndReloadSave);

            for (int i = 0; i < saveSlotButtons.Count; i++)
            {
                saveSlotButtons[i].Button.onClick.RemoveAllListeners();
            }
        }

        private void OnLoad()
        {
            SaveMaster.LoadSlot(SaveMaster.GetActiveSlot(), this.gameObject.scene.name);
        }

        private void OnSave()
        {
            SaveMaster.SyncSave();

            SaveMaster.WriteActiveSaveToDisk();

            UpdateActiveSlotButtonInfo();
        }

        private void WipeAndReloadSave()
        {
            int slot = SaveMaster.GetActiveSlot();
            SaveMaster.DeleteActiveSaveGame();
            SaveMaster.LoadSlot(slot, this.gameObject.scene.name);
        }

        private void OnChangeCharacterSprite(float value)
        {
            int index = Mathf.RoundToInt(value);

            if (character != null)
            {
                character.CharacterSpriteIndex = index;
            }

            SaveMaster.SyncSave();

            UpdateActiveSlotButtonInfo();
        }

        private void OnChangeCharacterName(string name)
        {
            character.CharacterDisplayName = name;

            SaveMaster.SyncSave();

            UpdateActiveSlotButtonInfo();
        }
    }
}
