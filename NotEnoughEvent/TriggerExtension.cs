using System.Text;
using UnityEngine;

namespace NEE
{
    public class TriggerExtension : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            //Debug.Log($"{other.gameObject.name} entered trigger on {gameObject.name} with parent {other.gameObject.transform.parent}");
            LevelEntity v = other.gameObject.GetComponent<LevelEntity>();
            if (v == null)
            {
                v = other.gameObject.transform.parent.GetComponentInChildren<LevelEntity>();
                if (v == null)
                {
                    v = other.gameObject.transform.parent.parent.GetComponentInParent<LevelEntity>();
                }
            }
            if (v != null)
            {
                v.ResetEntity(0);
                
                // if (v.behaviour?.Rigidbody != null)
                // {
                //     v.behaviour.Rigidbody.velocity = Vector3.zero;
                //     v.behaviour.Rigidbody.angularVelocity = Vector3.zero;
                // }
            }
        }
    }

}