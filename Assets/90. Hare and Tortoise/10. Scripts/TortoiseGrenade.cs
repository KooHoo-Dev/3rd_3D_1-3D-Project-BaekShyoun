using Unity.Cinemachine;
using UnityEngine;

namespace HareAndTortoise
{
    public class TortoiseGrenade : MonoBehaviour
    {
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private float explosionEffectLifetime = 5f;
        [SerializeField] private AudioClip explosionSound;
        [SerializeField, Range(0f, 1f)] private float explosionVolume = 1f;
        private bool hasExploded;
        private CinemachineImpulseSource impulseSource;

        private void Awake()
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (hasExploded) return;
            if (collision.gameObject.layer != LayerMask.NameToLayer("Ground")) return;
            Explode();
        }

        private void Explode()
        {
            if (RaceManager.Instance.CanAttack == false) return;
            hasExploded = true;
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            
            foreach (Collider hit in hits)
            {
                HareController hare = hit.GetComponent<HareController>();

                if (hare == null) continue;

                hare.ApplyKnockback(transform.position);
                break;
            }
            
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(
                    explosionEffectPrefab,
                    transform.position,
                    Quaternion.identity);

                Destroy(effect, explosionEffectLifetime);
            }
            
            
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(
                    explosionSound,
                    transform.position,
                    explosionVolume);
            }

            impulseSource.GenerateImpulse(new Vector3((Random.value < 0.5f ? -1 : 1), 1, 0));
            Destroy(gameObject);
        }
    }
}