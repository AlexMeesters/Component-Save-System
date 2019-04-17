using UnityEngine;

namespace Lowscope.Saving.Demo.Environment
{
    public class SaveDemoCoin : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                GameObject.Destroy(this.gameObject);
            }   
        }
    }
}

