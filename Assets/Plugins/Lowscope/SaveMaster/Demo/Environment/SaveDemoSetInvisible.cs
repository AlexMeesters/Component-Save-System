using UnityEngine;

namespace Lowscope.Saving.Demo.Environment
{
    public class SaveDemoSetInvisible : MonoBehaviour
    {
        [SerializeField] private GameObject target;

        private void OnCollisionEnter(Collision collision)
        {
            // Flip the visibility
            target.SetActive(!target.activeSelf);
        }

    }
}
