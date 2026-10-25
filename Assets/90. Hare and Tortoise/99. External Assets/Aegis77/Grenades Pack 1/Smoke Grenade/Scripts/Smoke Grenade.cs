using UnityEngine;
using System.Collections;

namespace Aegis.GrenadeSystem.Pack1
{

/// <summary>
/// Handles smoke screen deployment. Keeps the physical grenade object in the world as a 
/// source anchor, then safely shuts down particle generation and delays cleanup until the smoke clears.
/// </summary>

    public class SmokeGrenade : GrenadeBase
    {




        protected override void Explode()
        {
            // Spawn the Smoke
            if (data.explosionEffectPrefab != null)
            {
                GameObject smoke = Instantiate(data.explosionEffectPrefab, transform.position, Quaternion.Euler(-90f, 0f, 0f), transform);
                
               
            }

            Debug.Log("<color=gray>[Smoke]</color> Smoke screen deployed.");
        }

        //Prevent the grenade body from disappearing immediately
        protected override void OnPostExplode()
        {
            StartCoroutine(CleanupAfterSmoke());
        }

        private IEnumerator CleanupAfterSmoke()
{
    // Wait for the active smoke duration
    yield return new WaitForSeconds(data.smokeDuration);

    ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
    
    if (ps != null)
    {
        //stop smoke effect before destroying the effect prefab
        ps.Stop();
        
        yield return new WaitForSeconds(10.0f); 
    }

    Destroy(gameObject);
}
    }
}