using UnityEngine;

namespace Aegis.GrenadeSystem.Pack1
{

/// <summary>
/// World interaction script placed on pickup prefabs. Detects player touch, increases 
/// the specific grenade type's ammo allocation within the GrenadeSystem, and triggers spatial audio.
/// </summary>

    public class GrenadePickup : MonoBehaviour
    {




        [Header("Pickup Settings")]
        [Tooltip("Assign the ScriptableObject for the grenade type you want this pickup prefab to give.")]
        public GrenadeData grenadeData;
        
        [Tooltip("How many grenades to add on pickup.")]
        public int amountToAdd = 1;

        [Header("Audio")]
        [Tooltip("The sound that plays when the player walks over the pickup.")]
        public AudioClip pickupSound;
        [Range(0f, 1f)] public float volume = 1f;

        private void OnTriggerEnter(Collider other)
        {
            // Check if the object collided with is the Player
            if (other.CompareTag("Player"))
            {
                GrenadeSystem system = other.GetComponent<GrenadeSystem>();

                if (system != null && grenadeData != null)
                {
                    // 1. Add the grenade to the inventory
                    system.PickupGrenade(grenadeData, amountToAdd);

                    //Play pickup sound
                    if (pickupSound != null)
                    {
                        AudioSource soundSource = other.GetComponent<AudioSource>();
                        soundSource.clip = pickupSound;
                        soundSource.Play();
                    }

                    //Remove the pickup from the scene
                    Destroy(gameObject);
                }
            }
        }
    }
}