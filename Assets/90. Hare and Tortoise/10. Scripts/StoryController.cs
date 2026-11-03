using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HareAndTortoise
{
    // 공통 Story 씬에서 선택된 TSV 대화만 표시하고, 종료 후 다음 씬으로 이동한다.
    public class StoryController : MonoBehaviour
    {
        [SerializeField] private TextAsset storyTable;
        [SerializeField] private TMP_Text characterText;
        [SerializeField] private GameObject characterBox;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private GameObject normalCharacters;
        [SerializeField] private GameObject humanoidCharacters;

        [Header("Appearance Transition")]
        [SerializeField] private Image appearanceFade;
        [SerializeField] private float appearanceFadeDuration = 0.5f;
        private bool isChangingAppearance;

        private readonly List<StoryLine> lines = new List<StoryLine>();
        private int lineIndex;
        private bool isFinished;
        private Renderer[] tortoiseRenderers;
        private Renderer[] hareRenderers;
        private Renderer[] normalTortoiseRenderers;
        private Renderer[] normalHareRenderers;
        private Renderer[] humanoidTortoiseRenderers;
        private Renderer[] humanoidHareRenderers;
        private MaterialPropertyBlock colorBlock;

        private struct StoryLine
        {
            public int Order;
            public string Speaker;
            public string Text;
        }

        private void Awake()
        {
            // Unity 리소스는 필드 초기화가 아니라 메인 스레드의 Awake에서 생성한다.
            colorBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (storyTable == null || characterText == null || lineText == null ||
                characterBox == null || normalCharacters == null || humanoidCharacters == null ||
                appearanceFade == null)
            {
                Debug.LogError("StoryController의 TSV·화자 박스·대사·캐릭터 참조를 연결해주세요.", this);
                enabled = false;
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            appearanceFade.gameObject.SetActive(false);

            // 네 모델의 렌더러는 한 번만 찾고, 화자가 바뀌면 사용할 쌍만 교체한다.
            normalTortoiseRenderers = normalCharacters.transform.Find("Tortoise_Left")
                .GetComponentsInChildren<Renderer>(true);
            normalHareRenderers = normalCharacters.transform.Find("Hare_Right")
                .GetComponentsInChildren<Renderer>(true);
            humanoidTortoiseRenderers = humanoidCharacters.transform.Find("Tortoise_Left")
                .GetComponentsInChildren<Renderer>(true);
            humanoidHareRenderers = humanoidCharacters.transform.Find("Hare_Right")
                .GetComponentsInChildren<Renderer>(true);

            // 첫 대사가 나레이션일 때 사용할 장면별 기본 모델 구성.
            SetCharacterGroup(SceneController.StoryId == "AfterTutorialC");

            using (var reader = new StringReader(storyTable.text))
            {
                if (reader.ReadLine() != "StoryId\tOrder\tSpeakerId\tText")
                {
                    Debug.LogError("스토리 TSV의 헤더를 확인해주세요.", this);
                    enabled = false;
                    return;
                }

                string row;
                while ((row = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(row)) continue;
                    string[] cells = row.Split(new[] { '\t' }, 4);
                    if (cells[0] != SceneController.StoryId) continue;
                    if (cells.Length != 4 || !int.TryParse(cells[1], out int order))
                    {
                        Debug.LogError("스토리 TSV 행 형식 오류: " + row, this);
                        enabled = false;
                        return;
                    }

                    lines.Add(new StoryLine { Order = order, Speaker = cells[2], Text = cells[3] });
                }
            }

            if (lines.Count == 0)
            {
                Debug.LogError("대사가 없습니다: " + SceneController.StoryId, this);
                enabled = false;
                return;
            }

            lines.Sort((a, b) => a.Order.CompareTo(b.Order));
            ShowLine();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                NextLine();
        }

        public void NextLine()
        {
            if (isFinished || isChangingAppearance || lines.Count == 0) return;
            SoundManager.Play(SoundEffect.DialogueNext);
            lineIndex++;
            if (lineIndex < lines.Count)
            {
                string speaker = lines[lineIndex].Speaker;
                if (normalCharacters.activeSelf &&
                    (speaker == "HumanoidHare" || speaker == "HumanoidTortoise"))
                    StartCoroutine(ChangeAppearance());
                else
                    ShowLine();
                return;
            }

            isFinished = true;
            SceneController.FinishStory();
        }

        // 현재 스토리만 종료한다. 다음 씬과 흑화 저장은 기존 종료 로직을 그대로 사용한다.
        public void SkipStory()
        {
            if (isFinished || isChangingAppearance || lines.Count == 0 || enabled == false) return;
            isFinished = true;
            SceneController.FinishStory();
        }

        private IEnumerator ChangeAppearance()
        {
            isChangingAppearance = true;
            appearanceFade.transform.SetAsLastSibling();
            appearanceFade.gameObject.SetActive(true);

            try
            {
                // 완전히 어두워진 뒤에만 모델과 다음 대사를 함께 교체한다.
                yield return FadeAppearance(0f, 1f);
                ShowLine();
                yield return FadeAppearance(1f, 0f);
            }
            finally
            {
                // 전환이 중단돼도 검은 화면과 입력 잠금이 남지 않도록 정리한다.
                if (appearanceFade != null) appearanceFade.gameObject.SetActive(false);
                isChangingAppearance = false;
            }
        }

        private IEnumerator FadeAppearance(float from, float to)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, appearanceFadeDuration);
            Color color = Color.black;
            color.a = from;
            appearanceFade.color = color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(from, to, elapsed / duration);
                appearanceFade.color = color;
                yield return null;
            }
        }

        private void ShowLine()
        {
            StoryLine line = lines[lineIndex];
            string speaker;
            bool hareSpeaking = false;
            bool tortoiseSpeaking = false;

            switch (line.Speaker)
            {
                case "Narrator":
                    // 직전 모델 구성은 유지하고 이름 박스만 숨긴다.
                    speaker = "";
                    break;
                case "Hare":
                    SetCharacterGroup(false);
                    speaker = "토끼";
                    hareSpeaking = true;
                    break;
                case "Tortoise":
                    SetCharacterGroup(false);
                    speaker = "거북이";
                    tortoiseSpeaking = true;
                    break;
                case "HumanoidHare":
                    SetCharacterGroup(true);
                    speaker = "토끼(?)";
                    hareSpeaking = true;
                    break;
                case "HumanoidTortoise":
                    SetCharacterGroup(true);
                    speaker = "거북이(?)";
                    tortoiseSpeaking = true;
                    break;
                default:
                    Debug.LogError("알 수 없는 스토리 화자: " + line.Speaker, this);
                    enabled = false;
                    return;
            }

            characterBox.SetActive(line.Speaker != "Narrator");
            characterText.SetText(speaker);
            lineText.SetText(line.Text);
            SetBrightness(tortoiseRenderers, tortoiseSpeaking ? 1f : 0.3f);
            SetBrightness(hareRenderers, hareSpeaking ? 1f : 0.3f);
        }

        private void SetCharacterGroup(bool useHumanoid)
        {
            normalCharacters.SetActive(useHumanoid == false);
            humanoidCharacters.SetActive(useHumanoid);
            tortoiseRenderers = useHumanoid ? humanoidTortoiseRenderers : normalTortoiseRenderers;
            hareRenderers = useHumanoid ? humanoidHareRenderers : normalHareRenderers;
        }
        
        private void SetBrightness(Renderer[] renderers, float brightness)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;

                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material == null) continue;

                    string property = material.HasProperty("_BaseColor")
                        ? "_BaseColor" : "_Color";

                    if (material.HasProperty(property) == false) continue;

                    Color color = material.GetColor(property);
                    color.r *= brightness;
                    color.g *= brightness;
                    color.b *= brightness;

                    colorBlock.Clear();
                    renderers[i].GetPropertyBlock(colorBlock, j);
                    colorBlock.SetColor(property, color);
                    renderers[i].SetPropertyBlock(colorBlock, j);
                }
            }
        }
    }
}
