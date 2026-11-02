using Unity.Cinemachine;
using UnityEngine;

namespace HareAndTortoise
{
    public class TortoiseLandmine : MonoBehaviour
    {
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
        
        private void OnTriggerEnter(Collider other)
        {
            if (RaceManager.Instance.CanAttack == false) return;
            if (hasExploded) return;

            HareController hare = other.GetComponent<HareController>();
            if (hare == null) return;
            hasExploded = true;
            hare.ApplyKnockback(transform.position);
            
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, explosionEffectLifetime);
            }

            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
            }
            
            impulseSource.GenerateImpulse(new Vector3((Random.value < 0.5f ? -1 : 1), 1, 0));
            Destroy(gameObject);
        }
    }
}