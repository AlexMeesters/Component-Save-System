using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Demo.Environment
{
    public class SaveDemoIncreaseCube : MonoBehaviour, ISaveable
    {
        [SerializeField] private TextMesh counter;
        private int count;

        private void OnCollisionEnter(Collision collision)
        {
            count++;
            counter.text = count.ToString();
        }

        public string OnSave()
        {
            return count.ToString();
        }

        public void OnLoad(string data)
        {
            count = int.Parse(data);
            counter.text = count.ToString();
        }

        public bool OnSaveCondition()
        {
            return true;
        }
    }
}
