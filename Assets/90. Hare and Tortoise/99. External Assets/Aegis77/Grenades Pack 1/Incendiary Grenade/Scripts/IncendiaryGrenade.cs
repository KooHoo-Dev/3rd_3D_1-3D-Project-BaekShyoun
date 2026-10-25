using UnityEngine;

namespace Aegis.GrenadeSystem.Pack1
{

/// <summary>
/// Handles incendiary deployment. Bypasses standard blast damage to spawn a 
/// persistent, ground-based area-of-effect flame zone.
/// </summary>

    public class IncendiaryGrenade : GrenadeBase
    {


        [Header("Incendiary Prefab")]
        [SerializeField] private GameObject fireZonePrefab;

        protected override void Explode()
        {
            //Spawn the persistent Fire effect
            if (fireZonePrefab != null)
            {
                // Instantiate at the grenade's position with the -90 degree floor rotation
                GameObject fireZone = Instantiate(fireZonePrefab, transform.position, Quaternion.Euler(-90f, 0f, 0f));
                
                //Pass data to the FireDamage script
                FireDamage fireLogic = fireZone.GetComponent<FireDamage>();
                if (fireLogic != null)
                {
                    fireLogic.fireDamage = (int)data.fireDamagePerTick;
                    fireLogic.damageInterval = data.damageInterval;
                    fireLogic.fireDuration = data.fireDuration;
                }

                //Clean up the fire effect
                
            }
        }
    }
}