using UnityEngine;
using UnityEngine.UI;

namespace Lowscope.Saving.Demo
{
    public class ExampleScript : MonoBehaviour
    {
        [SerializeField] private Button buttonTestSyncSave;
        [SerializeField] private Button buttonTestSyncLoad;
        [SerializeField] private Button buttonTestSyncSaveWrite;
        [SerializeField] private Button buttonTestSyncSaveLoad;
        [SerializeField] private Button buttonRandomizeTransforms;
        [SerializeField] private Button buttonWipeSave;
        [SerializeField] private Text displayText;

        // For scene reload
        private static float lastestSpeed;

        private void Awake()
        {
            buttonTestSyncSave.onClick.AddListener(SyncSave);
            buttonTestSyncLoad.onClick.AddListener(SyncLoad);
            buttonTestSyncSaveWrite.onClick.AddListener(SyncSaveWrite);
            buttonTestSyncSaveLoad.onClick.AddListener(SyncSaveLoad);
            buttonRandomizeTransforms.onClick.AddListener(RandomizeTransforms);
            buttonWipeSave.onClick.AddListener(WipeSave);

            displayText.text = lastestSpeed.ToString();
        }

        private void SyncSave()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.SyncSave();

            stopWatch.Stop();
            displayText.text = stopWatch.ElapsedMilliseconds.ToString();
        }

        private void SyncLoad()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.SyncLoad();

            stopWatch.Stop();
            displayText.text = stopWatch.ElapsedMilliseconds.ToString();
        }

        private void SyncSaveWrite()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.WriteActiveSaveToDisk();

            stopWatch.Stop();
            displayText.text = stopWatch.ElapsedMilliseconds.ToString();
        }

        private void SyncSaveLoad()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.LoadSlot(SaveMaster.GetActiveSlot(), this.gameObject.scene.name);

            stopWatch.Stop();
            displayText.text = stopWatch.ElapsedMilliseconds.ToString();
            lastestSpeed = stopWatch.ElapsedMilliseconds;
        }

        private void RandomizeTransforms()
        {
            foreach (var item in this.gameObject.scene.GetRootGameObjects())
            {
                item.transform.Translate(UnityEngine.Random.insideUnitSphere);
                item.transform.Rotate(UnityEngine.Random.insideUnitSphere);
                item.transform.localScale = UnityEngine.Random.insideUnitSphere;
            }
        }

        private void WipeSave()
        {
            SaveMaster.DeleteActiveSaveGame();
            displayText.text = "Wiped save, created new save at slot 0";
            lastestSpeed = 0;

            SaveMaster.LoadSlot(0);
        }
    }
}