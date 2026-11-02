using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HareAndTortoise
{
    // 공통 Story 씬에서 선택된 TSV 대화만 표시하고, 종료 후 다음 씬으로 이동한다.
    public class StoryController : MonoBehaviour
    {
        [SerializeField] private TextAsset storyTable;
        [SerializeField] private TMP_Text characterText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private GameObject normalCharacters;
        [SerializeField] private GameObject humanoidCharacters;

        private readonly List<StoryLine> lines = new List<StoryLine>();
        private int lineIndex;
        private bool isFinished;
        private Renderer[] tortoiseRenderers;
        private Renderer[] hareRenderers;

        private struct StoryLine
        {
            public int Order;
            public string Speaker;
            public string Text;
        }

        private void Start()
        {
            if (storyTable == null || characterText == null || lineText == null)
            {
                Debug.LogError("StoryController의 TSV·화자·대사 참조를 연결해주세요.", this);
                enabled = false;
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            bool useHumanoid = SceneController.StoryId == "AfterTutorialC";
            normalCharacters.SetActive(useHumanoid == false);
            humanoidCharacters.SetActive(useHumanoid);
            
            Transform characters = useHumanoid ? humanoidCharacters.transform : normalCharacters.transform;

            tortoiseRenderers = characters.Find("Tortoise_Left").GetComponentsInChildren<Renderer>(true);
            hareRenderers = characters.Find("Hare_Right").GetComponentsInChildren<Renderer>(true);

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
            if (isFinished || lines.Count == 0) return;
            lineIndex++;
            if (lineIndex < lines.Count)
            {
                ShowLine();
                return;
            }

            isFinished = true;
            SceneController.FinishStory();
        }

        private void ShowLine()
        {
            StoryLine line = lines[lineIndex];
            string speaker = line.Speaker == "Hare" ? "토끼"
                : line.Speaker == "Tortoise" ? "거북이" : line.Speaker;
            characterText.SetText(speaker);
            lineText.SetText(line.Text);
            SetBrightness(tortoiseRenderers, line.Speaker == "Tortoise" ? 1f : 0.3f);
            SetBrightness(hareRenderers, line.Speaker == "Hare" ? 1f : 0.3f);

        }
        
        private void SetBrightness(Renderer[] renderers, float brightness)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

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

                    block.Clear();
                    renderers[i].GetPropertyBlock(block, j);
                    block.SetColor(property, color);
                    renderers[i].SetPropertyBlock(block, j);
                }
            }
        }
    }
}
