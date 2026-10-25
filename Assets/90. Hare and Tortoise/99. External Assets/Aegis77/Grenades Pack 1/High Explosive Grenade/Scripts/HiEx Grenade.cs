using UnityEngine;

namespace Aegis.GrenadeSystem.Pack1
{


/// <summary>
/// Handles standard high-explosive blast logic. Delivers heavy immediate radial damage, 
/// calculates line-of-sight occlusions, and applies high physical impact forces to nearby objects.
/// </summary>

    public class HiExGrenade : GrenadeBase
    {
        protected override void Explode()
        {
            // Find everything within the maximum damage radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, data.farRadius);

            foreach (Collider hit in colliders)
            {

                // Apply Physics Force
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(data.explosionForce, transform.position, data.explosionRadius);
                }

                /* * TARGET FILTERING FOR DAMAGE:
                 * By default, this script only affects the "Player". 
                 * To allow this grenade to damage other objects (like enemies), 
                 * you can change the tag below to "Enemy", or remove this IF statement 
                 */
                if (!hit.CompareTag("Player")) continue;

                // Line of Sight Check (Ensure no walls are in the way)
                Vector3 direction = hit.transform.position - transform.position;
                if (Physics.Raycast(transform.position, direction, out RaycastHit wallHit, data.farRadius))
                {
                    if (wallHit.collider != hit) continue;
                }

                // Calculate distance and determine zone damage
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float damageToApply = CalculateZoneDamage(distance);

                // Console Message currently communicates damage done
                //UPDATE THIS TO APPLY YOUR OWN DAMAGE LOGIC
                Debug.Log($"<color=red>[Damage]</color> Player hit for {damageToApply}!");

                
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