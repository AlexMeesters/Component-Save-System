using Lowscope.Saving.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Lowscope.Saving.Demo
{
    public class SaveSpeedTest : MonoBehaviour
    {
        [SerializeField] private Button buttonTestSyncSave = null;
        [SerializeField] private Button buttonTestSyncLoad = null;
        [SerializeField] private Button buttonTestSyncSaveWrite = null;
        [SerializeField] private Button buttonTestSyncSaveLoad = null;
        [SerializeField] private Button buttonRandomizeTransforms = null;
        [SerializeField] private Button buttonWipeAllSaveablesData = null;
        [SerializeField] private Button buttonWipeSave = null;
        [SerializeField] private Text displayText = null;

        [SerializeField] private GameObject saveablesContainer = null;
        private Saveable[] saveables;

        // For scene reload
        private static float lastestSpeed;

        private void Awake()
        {
            saveables = saveablesContainer.GetComponentsInChildren<Saveable>();

            buttonTestSyncSave.onClick.AddListener(SyncSave);
            buttonTestSyncLoad.onClick.AddListener(SyncLoad);
            buttonTestSyncSaveWrite.onClick.AddListener(SyncSaveWrite);
            buttonTestSyncSaveLoad.onClick.AddListener(SyncSaveLoad);
            buttonRandomizeTransforms.onClick.AddListener(RandomizeTransforms);
            buttonWipeSave.onClick.AddListener(WipeSave);
            buttonWipeAllSaveablesData.onClick.AddListener(WipeSaveables);

            displayText.text = lastestSpeed.ToString();
        }

        private void SyncSave()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.SyncSave();

            stopWatch.Stop();
            displayText.text = stopWatch.Elapsed.TotalMilliseconds.ToString();
        }

        private void SyncLoad()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.SyncLoad();

            stopWatch.Stop();
            displayText.text = stopWatch.Elapsed.TotalMilliseconds.ToString();
        }

        private void SyncSaveWrite()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.WriteActiveSaveToDisk();

            stopWatch.Stop();
            displayText.text = stopWatch.Elapsed.TotalMilliseconds.ToString();
        }

        private void SyncSaveLoad()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            SaveMaster.ClearSlot();
            SaveMaster.SetSlot(0, true);

            stopWatch.Stop();
            displayText.text = stopWatch.Elapsed.TotalMilliseconds.ToString();
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

        private void WipeSaveables()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            int saveableCount = saveables.Length;
            for (int i = 0; i < saveableCount; i++)
            {
                SaveMaster.WipeSaveable(saveables[i]);
            }

            stopWatch.Stop();
            displayText.text = stopWatch.Elapsed.TotalMilliseconds.ToString();
            lastestSpeed = stopWatch.ElapsedMilliseconds;
        }

        private void WipeSave()
        {
            SaveMaster.DeleteSave();
            displayText.text = "Wiped save, created new save at slot 0";
            lastestSpeed = 0;

            SaveMaster.SetSlot(0, true);
        }
    }
}