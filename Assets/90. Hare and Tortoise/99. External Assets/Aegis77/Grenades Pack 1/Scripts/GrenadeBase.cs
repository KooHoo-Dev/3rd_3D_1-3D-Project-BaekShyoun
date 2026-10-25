using UnityEngine;
using System.Collections;

namespace Aegis.GrenadeSystem.Pack1
{

/// <summary>
/// Abstract parent class attached to grenade prefabs. Manages the shared life cycle, 
/// initial physics, timing countdowns, and impact audio before triggering the specific explosion.
/// </summary>

    // Every grenade requires a Rigidbody for physics and an AudioSource for impact sounds
    [RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
    public abstract class GrenadeBase : MonoBehaviour
    {



        [Header("Base Settings")]
        [SerializeField] protected GrenadeData data;

        protected float countdown;
        protected bool hasExploded = false;
        protected AudioSource localAudioSource;

        protected virtual void Awake()
        {
            localAudioSource = GetComponent<AudioSource>();
        }

        protected virtual void Start()
        {
            // Initialize the timer from ScriptableObject data
            if (data != null)
                countdown = data.explosionDelay;
            else
                Debug.LogError($"{gameObject.name} is missing GrenadeData!");
        }

        protected virtual void Update()
        {
            if (hasExploded) return;

            countdown -= Time.deltaTime;
            if (countdown <= 0)
            {
                PerformExplosionSequence();
            }
        }

        private void PerformExplosionSequence()
        {
            hasExploded = true;

            // 1. Spawn common visual effects
            if (data.explosionEffectPrefab != null)
            {
                GameObject effectInstance = Instantiate(data.explosionEffectPrefab, transform.position + data.effectOffset, Quaternion.identity);

                Destroy(effectInstance, data.explosionEffectLifetime);
            }

            // 2. Play the explosion sound
            PlayExplosionSound();

            // 3. RUN UNIQUE LOGIC (This calls the specific code in Frag, Smoke, etc.)
            Explode();

            // 4. Final Cleanup
            // We usually destroy the grenade object immediately, 
            // unless a specific grenade (like Smoke) needs to stick around.
            OnPostExplode();
        }

        // This is the "Plug" where the specific grenade logic goes
        protected abstract void Explode();

        // Optional: Override this if a grenade needs to stay in the scene (like Smoke)
        protected virtual void OnPostExplode()
        {
            Destroy(gameObject);
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            
            if (!collision.gameObject.CompareTag("Player") && data.impactSound != null)
            {
                localAudioSource.PlayOneShot(data.impactSound);
            }
        }

        private void PlayExplosionSound()
        {
            if (data.explosionSounds.Length == 0 || data.audioSourcePrefab == null) return;

            // Instantiate the sound object at the explosion point
            GameObject snd = Instantiate(data.audioSourcePrefab, transform.position, Quaternion.identity);
            AudioSource audio = snd.GetComponent<AudioSource>();

            // Pick a random sound from the array in our ScriptableObject
            audio.clip = data.explosionSounds[Random.Range(0, data.explosionSounds.Length)];
            audio.Play();

            // Clean up the sound object after the clip finishes
            Destroy(snd, audio.clip.length);
        }
    }
}