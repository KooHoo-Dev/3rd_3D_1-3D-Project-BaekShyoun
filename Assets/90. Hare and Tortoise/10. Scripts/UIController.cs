using TMPro;
using UnityEngine;

namespace HareAndTortoise
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text grenadeCountText;
        [SerializeField] private TMP_Text landmineCountText;
        [SerializeField] private GameObject inventoryImage;
        
        private TortoiseController tortoise;

        private void Start()
        {
            tortoise = FindFirstObjectByType<TortoiseController>();
            RefreshCounts();
        }
        
        private void LateUpdate()
        {
            bool showInventory =
                RaceManager.Instance.CanAttack && (RaceManager.Instance.IsPreParing || tortoise.IsAiming);

            if (inventoryImage.activeSelf != showInventory) inventoryImage.SetActive(showInventory);
        }

        public void RefreshCounts()
        {
            grenadeCountText.text = $"{RaceManager.Instance.GrenadeCount}";
            landmineCountText.text = $"{RaceManager.Instance.LandmineCount}";
        }
    }
}