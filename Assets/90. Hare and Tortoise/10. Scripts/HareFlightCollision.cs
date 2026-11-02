using UnityEngine;

namespace HareAndTortoise
{
    public class HareFlightCollision : MonoBehaviour
    {
        private HareController hare;

        private void Awake()
        {
            hare = GetComponentInParent<HareController>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hare == null) return;

            hare.NotifyFlightCollision(collision);
        }
    }
}