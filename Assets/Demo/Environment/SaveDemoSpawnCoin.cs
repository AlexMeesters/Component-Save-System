using Lowscope.Saving.Enums;
using Lowscope.Saving;
using UnityEngine;

namespace Lowscope.Saving.Demo.Environment
{
    public class SaveDemoSpawnCoin : MonoBehaviour
    {
        [SerializeField] private Transform spawnLocation;

        private void OnCollisionEnter(Collision collision)
        {
            var obj = SaveMaster.SpawnObject(InstanceSource.Resources, "Coin");

            if (obj != null)
            {
                obj.transform.position = spawnLocation.position;
            }
        }
    }
}