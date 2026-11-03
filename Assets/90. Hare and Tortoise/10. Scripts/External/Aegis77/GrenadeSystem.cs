using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace Aegis.GrenadeSystem.Pack1
{


/// <summary>
/// The primary player inventory manager. Handles dynamic selection switching via the 
/// mouse scroll wheel or number hotkeys, tracks ammo per slot, updates the HUD, and spawns thrown prefabs.
/// </summary>

    // A small helper class to track ammo per type
    [System.Serializable]
    public class GrenadeSlot
    {
        public GrenadeData data;
        public int count;
       
    }

    public class GrenadeSystem : MonoBehaviour
    {


        [Header("Inventory")]
        public List<GrenadeSlot> inventory = new List<GrenadeSlot>();
        private int currentIndex = 0;

        [Header("Throw Settings")]
        public Transform throwPoint;
        public KeyCode throwKey = KeyCode.G;

        [Header("UI Reference")]
        public Image activeGrenadeIcon; // UI element to show the currently selected grenade's icon
        public TMPro.TextMeshProUGUI ammoText; // UI element to show the count
        public TMPro.TextMeshProUGUI nameText; // UI element to show the count


        void Start() => UpdateUI();

        void Update()
        {
            HandleScrollInput();
            HandleNumberKeyInput();

            if (Input.GetKeyDown(throwKey))
            {
                ThrowGrenade();
            }
        }

        private void HandleNumberKeyInput()
        {
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i < inventory.Count)
                {
                    currentIndex = i;
                    UpdateUI();
                }
            }
        }

        private void HandleScrollInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) SwitchGrenade(1);
            else if (scroll < 0f) SwitchGrenade(-1);
        }

        private void SwitchGrenade(int direction)
        {
            if (inventory.Count <= 1) return;
            currentIndex = (currentIndex + direction + inventory.Count) % inventory.Count;
            UpdateUI();
        }

        //Pickup Grenade
        public void PickupGrenade(GrenadeData dataToFind, int amount) 
        {
            if (dataToFind == null) return;

            foreach (GrenadeSlot slot in inventory)
            {
                if (slot.data == dataToFind)
                {
        
                    slot.count += amount; 
                    UpdateUI();
                    Debug.Log($"<color=green>[Inventory]</color> Added {amount} {dataToFind.grenadeName}. Total: {slot.count}");
                    return;
                }
            }

            UpdateUI();
        }

        private void ThrowGrenade()
        {
            if (inventory.Count == 0) return;

            GrenadeSlot currentSlot = inventory[currentIndex];

            // Check grenade count for currentlyselected grenade
            if (currentSlot.count <= 0)
            {
                Debug.LogWarning("Out of grenades of this type!");
                return;
            }

            // 1. Spawn
            GameObject grenadeObj = Instantiate(currentSlot.data.prefab, throwPoint.position, throwPoint.rotation);
            
            //Physics
            Rigidbody rb = grenadeObj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(throwPoint.forward * currentSlot.data.throwForce, ForceMode.VelocityChange);

            // Decrease grenade count
            currentSlot.count--;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (inventory.Count == 0) return;

            GrenadeSlot current = inventory[currentIndex];
            if (activeGrenadeIcon != null) activeGrenadeIcon.sprite = current.data.icon;
            if (ammoText != null) ammoText.text = current.count.ToString();
            if (nameText != null) nameText.text = current.data.grenadeName.ToString();
        }
    }
}