using UnityEngine;

namespace Aegis.GrenadeSystem.Pack1
{

/// <summary>
/// Handles fragmentation grenade explosion logic. Fires a burst of raycasts (shrapnel) outward from 
/// the blast center, applying damage, impact effects, and physical force to hit targets.
/// </summary>

    public class FragmentationGrenade : GrenadeBase
    {


        protected override void Explode()
        {
            // 1. Explosion
            ApplyExplosionPhysics();

            // 2. Fragmentation Shrapnel
            SpawnShrapnel();
        }

        private void ApplyExplosionPhysics()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, data.farRadius);
            foreach (Collider hit in colliders)
            {
                // Line of Sight check for the physical blast
                Vector3 direction = hit.transform.position - transform.position;
                if (Physics.Raycast(transform.position, direction, out RaycastHit wallHit, data.farRadius))
                {
                    if (wallHit.collider != hit) continue;
                }

                // Push objects
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null) rb.AddExplosionForce(data.explosionForce, transform.position, data.explosionRadius);

                // Apply Zone Damage

                /* * TARGET FILTERING FOR DAMAGE:
                 * By default, this script only affects the "Player". 
                 * To allow this grenade to damage other objects (like enemies), 
                 * you can change the tag below to include "Enemy" etc
                 */
                if (hit.CompareTag("Player"))
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float damage = CalculateZoneDamage(distance);

                    // Console Message currently communicates damage done
                    //UPDATE THIS TO APPLY YOUR OWN DAMAGE LOGIC
                    Debug.Log($"<color=red>[Frag Damage]</color> Player hit by blast for {damage}!");
                }
            }
        }

        private void SpawnShrapnel()
        {
            for (int i = 0; i < data.shrapnelCount; i++)
            {
                Vector3 randomDirection = Random.onUnitSphere;

                if (Physics.Raycast(transform.position, randomDirection, out RaycastHit hitInfo, data.shrapnelRange))
                {
                    // Visuals for tracing the shrapnel trajectory in inspector
                    Debug.DrawLine(transform.position, hitInfo.point, Color.green, 2.0f);

                    if (data.sparkEffectPrefab != null)
                    {
                        GameObject spark = Instantiate(data.sparkEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));

                        Destroy(spark, data.sparkLifetime);
                    }

                    // Damage to Player only
                    if (hitInfo.collider.CompareTag("Player"))
                    {

                        // Console Message currently communicates damage done
                        //UPDATE THIS TO APPLY YOUR OWN DAMAGE LOGIC
                        Debug.Log($"<color=red>[Shrapnel]</color> Player hit by shrapnel for {data.shrapnelDamage}!");
                    }
                }
            }
        }

        private float CalculateZoneDamage(float distance)
        {
            if (distance <= data.closeRadius) return data.closeDam;
            if (distance <= data.nearRadius) return data.nearDam;
            return data.farDam;
        }
    }
}