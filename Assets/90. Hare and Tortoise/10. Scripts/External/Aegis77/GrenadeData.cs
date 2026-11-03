using UnityEngine;

namespace Aegis.GrenadeSystem.Pack1
{


/// <summary>
/// Base ScriptableObject container that holds all shared configurations, 
/// physics settings, and visual/audio assets for a grenade type.
/// </summary>

    // This attribute allows you to right-click in the Project window to create new grenade files
    //use this if you'd like to create your own custom grenades!
    [CreateAssetMenu(fileName = "NewGrenadeData", menuName = "Aegis/Grenade Data")]
    public class GrenadeData : ScriptableObject
    {


        [Header("Identity")]
        public string grenadeName;
        public Sprite icon;
        public GameObject prefab;

        [Header("Physics & Timing")]
        public float explosionDelay = 3f;
        public float throwForce = 20f;
        public float explosionForce = 1000f;
        public float explosionRadius = 5f;

        [Header("Visuals")]
        public GameObject explosionEffectPrefab;
        public Vector3 effectOffset = new Vector3(0, 0.5f, 0);
        public float explosionEffectLifetime = 5f;

        [Header("Audio")]
        public AudioClip impactSound;
        public AudioClip[] explosionSounds;
        public GameObject audioSourcePrefab;

        [Header("Damage Settings")]
        public float closeRadius = 1f;
        public float nearRadius = 5f;
        public float farRadius = 7f;
        public float closeDam = 20f;
        public float nearDam = 10f;
        public float farDam = 2f;

        [Header("Shrapnel Settings")]
        public int shrapnelCount = 40;
        public float shrapnelRange = 15f;
        public int shrapnelDamage = 10;
        public GameObject sparkEffectPrefab;
        public float sparkLifetime = 3.0f;


        [Header("Flashbang Settings")]
        public float flashDuration = 2.5f;
        [Range(0f, 1f)] public float maxFlashAlpha = 1f;

        [Header("Incendiary Settings")]
        public float fireDuration = 10f;
        public float damageInterval = 1f;
        public int fireDamagePerTick = 5;

        [Header("Smoke Settings")]
        public float smokeDuration = 50f;


    }
}