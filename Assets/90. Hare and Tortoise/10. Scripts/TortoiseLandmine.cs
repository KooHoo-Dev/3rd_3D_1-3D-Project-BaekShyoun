using UnityEngine;

namespace HareAndTortoise
{
    public class TortoiseLandmine : MonoBehaviour
    {
        private bool hasExploded;

        private void OnTriggerEnter(Collider other)
        {
            if (hasExploded) return;

            HareController hare = other.GetComponent<HareController>();
            if (hare == null) return;
            hasExploded = true;
            hare.ApplyKnockback();
            Destroy(gameObject);
            
        }
    }
}