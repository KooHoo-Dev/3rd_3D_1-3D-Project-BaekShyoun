using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HareAndTortoise
{
    // 배치와 글꼴은 씬에서 수정하고, 이 스크립트는 선택·잠금·진입만 관리한다.
    public class StageSelectionController : MonoBehaviour
    {
        [SerializeField] private Button[] stageButtons;
        [SerializeField] private TMP_Text[] stageLabels;
        [SerializeField] private TMP_Text selectedText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private Color selectedButtonColor = new Color32(224, 177, 106, 255);
        private ColorBlock[] defaultButtonColors;
        private Color[] defaultLabelColors;

        private readonly string[] stageNames =
            { "Tutorial A", "Tutorial B", "Tutorial C", "Stage 1", "Stage 2", "Stage 3" };
        private readonly string[] sceneNames =
            { "00. Tutorial A", "01. Tutorial B", "02. Tutorial C", "03. Stage 1" };
        private int selectedIndex = -1;
        private bool isLoading;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            defaultButtonColors = new ColorBlock[stageButtons.Length];
            defaultLabelColors = new Color[stageLabels.Length];
            for (int i = 0; i < stageButtons.Length; i++)
            {
                defaultButtonColors[i] = stageButtons[i].colors;
                defaultLabelColors[i] = stageLabels[i].color;
                int index = i;
                stageButtons[i].onClick.AddListener(() => SelectStage(index));
            }
            startButton.onClick.AddListener(StartSelectedStage);
            titleButton.onClick.AddListener(ReturnToTitle);

            // 아직 클리어하지 않은 해금 구간을 우선 선택한다.
            for (int i = 0; i < sceneNames.Length; i++)
            {
                StageType stage = (StageType)i;
                if (StageProgress.IsUnlocked(stage) && StageProgress.IsCleared(stage) == false)
                {
                    selectedIndex = i;
                    break;
                }
            }
            if (selectedIndex < 0) selectedIndex = sceneNames.Length - 1;
            RefreshSelection();
        }

        public void SelectStage(int index)
        {
            if (isLoading || index < 0 || index >= sceneNames.Length) return;
            if (StageProgress.IsUnlocked((StageType)index) == false) return;
            selectedIndex = index;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < stageButtons.Length; i++)
            {
                bool exists = i < sceneNames.Length;
                bool unlocked = exists && StageProgress.IsUnlocked((StageType)i);
                string status = unlocked
                    ? (StageProgress.IsCleared((StageType)i) ? "클리어" : "미클리어")
                    : "잠김";
                stageLabels[i].SetText(stageNames[i] + "\n<size=17>" + status +
                    (exists ? "" : " · 준비 중") + "</size>");
                stageLabels[i].color = unlocked && i == selectedIndex
                    ? new Color32(28, 48, 38, 255)
                    : (unlocked ? defaultLabelColors[i] : new Color32(143, 155, 143, 255));
                stageButtons[i].interactable = unlocked;

                // 마우스가 다른 버튼이나 시작 버튼으로 옮겨져도 현재 선택 색을 유지한다.
                ColorBlock colors = defaultButtonColors[i];
                if (unlocked && i == selectedIndex)
                {
                    colors.normalColor = selectedButtonColor;
                    colors.highlightedColor = Color.Lerp(selectedButtonColor, Color.white, 0.1f);
                    colors.pressedColor = Color.Lerp(selectedButtonColor, Color.black, 0.15f);
                    colors.selectedColor = selectedButtonColor;
                }
                stageButtons[i].colors = colors;
            }

            selectedText.SetText(stageNames[selectedIndex] + " 선택됨\n" +
                (StageProgress.IsCleared((StageType)selectedIndex)
                    ? "클리어한 경주입니다. 다시 플레이할 수 있습니다."
                    : "선택한 경주를 시작할 수 있습니다."));
            startButton.interactable = StageProgress.IsUnlocked((StageType)selectedIndex);
        }

        public void StartSelectedStage()
        {
            if (isLoading || selectedIndex < 0 || selectedIndex >= sceneNames.Length) return;
            if (StageProgress.IsUnlocked((StageType)selectedIndex) == false) return;
            isLoading = true;
            // 튜토리얼은 선택 화면에서 다시 시작해도 해당 도입 스토리부터 재생한다.
            switch ((StageType)selectedIndex)
            {
                case StageType.TutorialA:
                    SceneController.PlayStory("BeforeTutorialA");
                    break;
                case StageType.TutorialB:
                    SceneController.PlayStory("BeforeTutorialB");
                    break;
                case StageType.TutorialC:
                    SceneController.PlayStory("BeforeTutorialC");
                    break;
                default:
                    SceneManager.LoadScene(sceneNames[selectedIndex]);
                    break;
            }
        }

        public void ReturnToTitle()
        {
            if (isLoading) return;
            isLoading = true;
            SceneManager.LoadScene("80. Title");
        }
    }
}
