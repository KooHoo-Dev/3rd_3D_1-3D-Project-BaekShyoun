# Aegis Grenade System - Pack 1

Thank you for choosing the **Aegis Grenade System (Pack 1)**.
This Asset has been engineered from the ground up with **highly scalable, data-driven architecture**.
By utilizing C# class inheritance alongside Unity ScriptableObjects, this asset pack provides an expandable foundation for any tactical shooter, RPG, or action game.

---

## How It Works

This Grenade system separates Data, Management, and Execution (Physics & Explosion Logic) into three cooperative layers:

1. Configuration (The Brains)
Everything begins with **GrenadeData** (ScriptableObjects).
Each grenade type (Hi-Ex, Frag, Flashbang, Incendiary, Smoke) has its own unique asset file where you configure its parameters:
-Visuals & Interface (Prefabs, UI Icons)
-General Physics (Throw Force, Weight)
-Audio profiles (Impact & Detonation sounds)
-Specialized parameters (Flash duration, fire tick rates, shrapnel counts)

2. Management & Selection (The Controller)
The player object holds the **GrenadeSystem** script.

It uses a custom container (GrenadeSlot) to map your ScriptableObject data directly to the player's inventory.
It handles hotkeys and mouse-wheel scrolling to cycle through available throwables dynamically.
When a player triggers a throw (currently mapped to the G key by default), it spawns a thrown prefab which contains the logic and effects specific to that grenade.

3. Execution & Life Cycle (The Grenade)
Once a grenade is spawned into the game world, scripts attatched to each grenade inheriting from the **GrenadeBase** script handle the rest.

This base script handles plays physical collision audio on impact and tracks the fuse countdown, while more specific behaviours located within the inheriting scripts assigned to each
specific grenade prefab handle their unique behaviours (e.g., **FragmentationGrenade** sends out raycasts for shrapnel; **SmokeGrenade** manages a persistent particle system; **FireDamage** tracks area-of-effect damage ticks),
executing the unique tactical behaviors of each grenade.

4. Replenishment (The Loop Close)
When a player walks over a pickup prefab assigned the **GrenadePickup** script in the game world, the pickup reads the designated **GrenadeData**,
 safely increments the player's localized inventory count.
 
 
 The Grenades included in this pack and their effects are:
 
 - Hi-Ex (High explosive Grenade)
 Creates a large damaging blast with phisics that can move objects with Rigidbodies assigned.
 
 - Smoke Grenade
 Creates a large, long-duration, obscuring smoke effect
 
 - Flashbang
 A 'Stun' grenade that makes the player screen flash white and display an after-image in greater degrees the closer the player is to it
 
 - Incendiary Grenade
 An area-effect grenade that spawns a field of damaging fire which applies a set amount of damage per 'tick' over time
 
 - Fragmentation Grenade
 Creates a smaller explosion than the Hi-Ex,but also sends out a scattering of sharpnel in random directions to cause damage. Where this shrapnel impacts,
 it creates a sparking effect
 
 
 The Scripts contained in this asset pack are:
 
PlayerController.cs
GrenadeData.cs
GrenadeSystem.cs
GrenadePickup.cs
GrenadeBase.cs
HiExGrenade.cs
FragmentationGrenade.cs
FlashbangGrenade.cs
IncendiaryGrenade.cs
FireDamage.cs
SmokeGrenade.cs


**To create your own custom grenades using this system**:
Right-click in the Project window and select Create > Aegis Grenades > New Grenade Data to generate a new ScriptableObject asset,
where you can configure parameters like throw force, fuse timers, damage, and audio clips directly in the Inspector.
Next, prepare your projectile by attaching the corresponding behavior script (e.g., FragmentationGrenade or FlashbangGrenade - or your own script) to a 3D model
with a Rigidbody and Collider.
Finally, save this object as a prefab, link it back into the Prefab slot of your new data asset, and drag that completed data asset into an open slot
on the player's GrenadeSystem component list to instantly register it into the UI and hotkey index.