using UnityEngine;

namespace HareAndTortoise
{
    public class TortoiseAnimation : MonoBehaviour
    {
        private TortoiseController tortoise;

        private void Awake()
        {
            tortoise = GetComponentInParent<TortoiseController>();
        }

        public void ReleaseGrenade()
        {
            tortoise.ReleaseGrenade();
        }
    }
}