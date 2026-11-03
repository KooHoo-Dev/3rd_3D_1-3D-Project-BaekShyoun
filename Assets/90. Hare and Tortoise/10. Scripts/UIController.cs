using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HareAndTortoise
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text grenadeCountText;
        [SerializeField] private TMP_Text landmineCountText;
        [SerializeField] private GameObject inventoryImage;
        [SerializeField] private GameObject resultImage;
        [SerializeField] private GameObject resultBackdrop;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button readySkipButton;
        [SerializeField] private TMP_Text preparationTimeText;
        [SerializeField] private TMP_Text raceText;
        [SerializeField] private GameObject tutorialSubtitlePanel;
        [SerializeField] private TMP_Text tutorialSubtitleText;
        private CancellationTokenSource subtitleCancellation;
        private int displayedPreparationSeconds = -1;
        
        private TortoiseController tortoise;

        private void Start()
        {
            tortoise = FindFirstObjectByType<TortoiseController>();
            RefreshCounts();
            resultImage.SetActive(false);
            resultBackdrop.SetActive(false);
            readySkipButton.gameObject.SetActive(RaceManager.Instance.CanSkipPreparation);
            RefreshPreparationTime();
        }
        
        private void LateUpdate()
        {
            bool showInventory =
                RaceManager.Instance.CanAttack && (RaceManager.Instance.IsPreParing || tortoise.IsAiming);

            if (inventoryImage.activeSelf != showInventory) inventoryImage.SetActive(showInventory);
            bool showReady = RaceManager.Instance.CanSkipPreparation;
            if (readySkipButton.gameObject.activeSelf != showReady)
                readySkipButton.gameObject.SetActive(showReady);
            if (showReady) RefreshPreparationTime();
        }

        private void RefreshPreparationTime()
        {
            int seconds = Mathf.CeilToInt(RaceManager.Instance.PreparationRemaining);
            if (displayedPreparationSeconds == seconds) return;
            displayedPreparationSeconds = seconds;
            preparationTimeText.SetText("준비 시간 {0}초", seconds);
        }

        public void ShowRaceText(string message)
        {
            bool show = string.IsNullOrEmpty(message) == false;
            if (show)
            {
                raceText.fontSize = message == "Start!" || int.TryParse(message, out _) ? 88f : 36f;
                raceText.SetText(message);
            }
            raceText.gameObject.SetActive(show);
        }

        public void RefreshCounts()
        {
            grenadeCountText.text = $"{RaceManager.Instance.GrenadeCount}";
            landmineCountText.text = $"{RaceManager.Instance.LandmineCount}";
        }
        
        public void ShowResult(string message)
        {
            StopTutorialSubtitles();
            resultText.SetText(message);
            resultBackdrop.SetActive(true);
            resultImage.SetActive(true);
        }

        // 준비 안내에서 경주 안내로 바뀌면 이전 자막을 취소하고 새 순서를 시작한다.
        public void ShowTutorialSubtitles(params string[] messages)
        {
            StopTutorialSubtitles();
            // 씬이 끝나면 OnDestroy에서 취소하므로 별도의 연결 토큰은 필요 없다.
            subtitleCancellation = new CancellationTokenSource();
            PlayTutorialSubtitles(messages, subtitleCancellation.Token);
        }

        private async UniTaskVoid PlayTutorialSubtitles(string[] messages, CancellationToken token)
        {
            tutorialSubtitlePanel.SetActive(true);
            for (int i = 0; i < messages.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                await TextManager.SetMessageAsync(tutorialSubtitleText, messages[i], 3f, token);
            }
            tutorialSubtitlePanel.SetActive(false);
        }

        private void StopTutorialSubtitles()
        {
            if (subtitleCancellation != null)
            {
                subtitleCancellation.Cancel();
                subtitleCancellation.Dispose();
                subtitleCancellation = null;
            }
            if (tutorialSubtitleText != null) TextManager.SetMessage(tutorialSubtitleText, false);
            if (tutorialSubtitlePanel != null) tutorialSubtitlePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            StopTutorialSubtitles();
        }
        
        public void OnReadySkip()
        {
            // Enter 등의 UI Submit이 아니라 실제 마우스 클릭으로만 준비시간을 건너뛴다.
            if (Mouse.current == null || Mouse.current.leftButton.wasReleasedThisFrame == false) return;
            if (RaceManager.Instance.CanSkipPreparation == false) return;
            RaceManager.Instance.SkipPreparation();
            readySkipButton.gameObject.SetActive(false);
        }

        public void OnResultYes()
        {
            RaceManager.Instance.OnResultYes();
        }

        public void OnResultNo()
        {
            RaceManager.Instance.OnResultNo();
        }
    }
}
