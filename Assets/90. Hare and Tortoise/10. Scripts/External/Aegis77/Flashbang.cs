using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Aegis.GrenadeSystem.Pack1
{

/// <summary>
/// Handles flashbang logic. Determines line-of-sight and proximity to the player 
/// to trigger a screen-blindness visual overlay.
/// </summary>

    public class Flashbang : GrenadeBase
    {



        private RawImage burnInOverlay;
        private Image whiteFlashOverlay;

        protected override void Explode()
        {
            //  Find the Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            // Check if player is in range (Using the 'FarRadius' from our Data SO)
            if (distance <= data.farRadius)
            {
                // Line of Sight Check (Can't be flashed through walls)
                Vector3 dir = player.transform.position - transform.position;
                if (Physics.Raycast(transform.position, dir, out RaycastHit hit, data.farRadius))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        SetupUIAndFlash();
                    }
                }
            }
        }

        private void SetupUIAndFlash()
        {
            // Finding the UI components
            GameObject burnObj = GameObject.Find("BurnInOverlay");
            GameObject flashObj = GameObject.Find("WhiteFlashOverlay");

            if (burnObj != null) burnInOverlay = burnObj.GetComponent<RawImage>();
            if (flashObj != null) whiteFlashOverlay = flashObj.GetComponent<Image>();

            if (burnInOverlay != null && whiteFlashOverlay != null)
            {
                // Start the Coroutine on a persistent object
                whiteFlashOverlay.StartCoroutine(ProcessFlashEffect());
            }
        }

        private IEnumerator ProcessFlashEffect()
        {
            // Capture Screen
            yield return new WaitForEndOfFrame();
            Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenTexture.Apply();

            // Apply to UI
            burnInOverlay.texture = screenTexture;
            SetAlpha(whiteFlashOverlay, data.maxFlashAlpha);
            SetAlpha(burnInOverlay, data.maxFlashAlpha);

            // Fade out
            float elapsed = 0;
            while (elapsed < data.flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / data.flashDuration;
                float currentAlpha = Mathf.Lerp(data.maxFlashAlpha, 0f, t);

                SetAlpha(whiteFlashOverlay, currentAlpha);
                SetAlpha(burnInOverlay, currentAlpha);
                yield return null;
            }

            //Cleanup
            SetAlpha(whiteFlashOverlay, 0f);
            SetAlpha(burnInOverlay, 0f);
            Destroy(screenTexture);
        }

        private void SetAlpha(Graphic g, float a)
        {
            Color c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}