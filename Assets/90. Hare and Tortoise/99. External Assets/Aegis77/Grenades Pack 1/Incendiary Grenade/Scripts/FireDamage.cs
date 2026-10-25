using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Aegis.GrenadeSystem.Pack1
{

/// <summary>
/// Attached to the incendiary flame prefab. Manages the damage-over-time tick intervals 
/// for characters standing in the radius, controls the hazard UI flash, and handles visual fade-out.
/// </summary>

    public class FireDamage : MonoBehaviour
    {



        [HideInInspector] public int fireDamage;
        [HideInInspector] public float damageInterval;
        [HideInInspector] public float fireDuration; 

        [Header("UI Settings")]
        public string damageIndicatorName = "DamageIndicator";
        private Image damageIndicator;

        private float nextDamageTime;
        private bool isPlayerInside = false;

        void Start()
        {
            // Find the UI once and cache it.
            GameObject uiObj = GameObject.Find(damageIndicatorName);
            if (uiObj != null)
            {
                damageIndicator = uiObj.GetComponent<Image>();
            }

            StartCoroutine(FadeAndDestroy());
        }

        void OnTriggerStay(Collider other)
        {
            // Only deal damage if the interval timer has passed
            if (other.CompareTag("Player") && Time.time >= nextDamageTime)
            {
                ApplyFireDamage();
                nextDamageTime = Time.time + damageInterval;
            }
        }

        private void ApplyFireDamage()
        {
            //Log the damage
            Debug.Log($"<color=orange>[Fire]</color> Player taking {fireDamage} burn damage!");

            // Trigger the UI flash if it exists
            if (damageIndicator != null)
            {
                StartCoroutine(FlashDamageUI());
            }
        }

        private System.Collections.IEnumerator FlashDamageUI()
        {
            // Simple logic to pulse the damage indicator - replace with your own effect!
            Color c = damageIndicator.color;
            c.a = 0.4f; 
            damageIndicator.color = c;

            while (c.a > 0)
            {
                c.a -= Time.deltaTime * 2f;
                damageIndicator.color = c;
                yield return null;
            }
        }


        private IEnumerator FadeAndDestroy()
{
    // Wait for the 'Burning' phase to end
    yield return new WaitForSeconds(fireDuration);

    ParticleSystem ps = GetComponent<ParticleSystem>();
    if (ps != null)
    {
        ps.Stop();
    }

    // Disable the collider
    Collider col = GetComponent<Collider>();
    if (col != null) col.enabled = false;

    // Wait for the flames to fully dissipate
    yield return new WaitForSeconds(6.0f);

    Destroy(gameObject);
}
    }
}