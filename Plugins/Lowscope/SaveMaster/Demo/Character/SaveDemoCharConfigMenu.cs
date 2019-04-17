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

        private static SaveDemoCharConfigMenu menu;

        private void Start()
        {
            if (menu == null)
            {
                GameObject.DontDestroyOnLoad(this.gameObject);
                menu = this;
            }
            else
            {
                if (character != null)
                {
                    menu.character = character;
                    menu.UpdateSlotInfo();
                }

                GameObject.Destroy(this.gameObject);
                return;
            }

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
                    StartCoroutine(LoadCoroutine(index));
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

        private IEnumerator LoadCoroutine(int slot, bool removeSave = false)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;

            // Due to Unity issues, unloading a scene doesn't work properly within the editor.
            // It's likely to work within builds.
            // When making your own game, this way of loading scenes isn't really nessacary.
            // It's reccomended to just use the Main Menu, set the slot there, and load the last active scene
            SceneManager.CreateScene("-");
            AsyncOperation op = SceneManager.UnloadSceneAsync(activeSceneName);

            while (!op.isDone)
            {
                yield return null;
            }

            SaveMaster.SetSlot(slot, false);

            SceneManager.LoadScene(activeSceneName);

            for (int i = 0; i < saveSlotButtons.Count; i++)
            {
                saveSlotButtons[i].UpdateInfo();
            }

            UpdateSlotInfo();
        }

        private void OnLoad()
        {
            StartCoroutine(LoadCoroutine(SaveMaster.GetActiveSlot()));
        }

        private void OnSave()
        {
            SaveMaster.Save();

            SaveMaster.WriteActiveSaveToDisk();

            UpdateActiveSlotButtonInfo();
        }

        private void WipeAndReloadSave()
        {
            int slot = SaveMaster.GetActiveSlot();
            SaveMaster.DeleteActiveSaveGame();
            StartCoroutine(LoadCoroutine(slot));
        }

        private void OnChangeCharacterSprite(float value)
        {
            int index = Mathf.RoundToInt(value);

            if (character != null)
            {
                character.CharacterSpriteIndex = index;
            }

            SaveMaster.Save();

            UpdateActiveSlotButtonInfo();
        }

        private void OnChangeCharacterName(string name)
        {
            character.CharacterDisplayName = name;

            SaveMaster.Save();

            UpdateActiveSlotButtonInfo();
        }
    }
}
