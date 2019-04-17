using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Demo.Character
{
    /// <summary>
    /// This is just an example to demonstrate how to utilize the save system
    /// The way resources are obtained isn't reccomended for production projects.
    /// Reasons for embedding the resources within this script is to keep the demo folder compact and simple.
    /// </summary>
    public class SaveDemoCharacter : MonoBehaviour, ISaveable
    {
        [Header("References - Visual")]
        [SerializeField]
        private new SpriteRenderer renderer = null;

        [Header("References - Functional")]
        [SerializeField]
        private Rigidbody rigidBody = null;

        [Header("Config")]
        [SerializeField]
        private float speed = 3;
        [SerializeField]
        private float jumpForce = 100;

        private int characterSpriteIndex = 0;
        private string characterDisplayName = "Default Name";
        private bool hasJumped;

        public string CharacterDisplayName
        {
            get { return characterDisplayName; }
            set { characterDisplayName = value; }
        }

        public int CharacterSpriteIndex
        {
            get { return characterSpriteIndex; }
            set
            {
                renderer.sprite = SaveDemoResources.GetCharacterSprite(value);
                characterSpriteIndex = value;
            }
        }

        private void Update()
        {
            if (!hasJumped)
                hasJumped = Input.GetKeyDown(KeyCode.Space);

            if (Input.GetKeyDown(KeyCode.Q))
            {
                UnityEngine.SceneManagement.SceneManager.CreateScene("fu");
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(gameObject.scene.name);
            }
        }

        void FixedUpdate()
        {
            Vector3 movement = Vector3.zero;
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.z = Input.GetAxisRaw("Vertical");

            movement.Normalize();
            movement *= speed;

            rigidBody.MovePosition(this.transform.position + (movement * Time.fixedDeltaTime));

            if (hasJumped)
            {
                rigidBody.WakeUp();
                rigidBody.AddForce(new Vector2(0, jumpForce), ForceMode.Acceleration);
                hasJumped = false;
            }
        }

        [System.Serializable]
        public struct SaveData
        {
            public int characterIndex;
            public string characterDisplayName;
        }

        public void OnLoad(string data)
        {
            SaveData saveData = (SaveData) JsonUtility.FromJson(data, typeof(SaveData));

            CharacterSpriteIndex = saveData.characterIndex;
            CharacterDisplayName = saveData.characterDisplayName;
        }

        public string OnSave()
        {
            return JsonUtility.ToJson(new SaveData()
            {
                characterIndex = this.characterSpriteIndex,
                characterDisplayName = this.characterDisplayName
            });
        }

        public bool OnSaveCondition()
        {
            return true;
        }

    }
}