using UnityEngine;
using UnityEngine.SceneManagement;

namespace HareAndTortoise
{
    // 스토리 선택과 종료 목적지만 관리한다. 캐릭터·경주 동작은 변경하지 않는다.
    public static class SceneController
    {
        private const string VillainKey = "HareAndTortoise.IsVillain";
        public static bool IsVillain => PlayerPrefs.GetInt(VillainKey, 0) == 1;
        public static string StoryId { get; private set; } = "BeforeTutorialA";

        public static void PlayStory(string storyId)
        {
            StoryId = storyId;
            SceneManager.LoadScene("90. Story");
        }

        public static void FinishStory()
        {
            switch (StoryId)
            {
                case "BeforeTutorialA":
                    SceneManager.LoadScene("00. Tutorial A");
                    break;
                case "BeforeTutorialB":
                    SceneManager.LoadScene("01. Tutorial B");
                    break;
                case "BeforeTutorialC":
                    // 흑화 후 타이틀 진입 시 저장해 다음 실행에서도 상태를 유지한다.
                    PlayerPrefs.SetInt(VillainKey, 1);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("80. Title");
                    break;
                case "AfterTutorialC":
                    SceneManager.LoadScene("03. Stage 1");
                    break;
            }
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(VillainKey);
            PlayerPrefs.Save();

            StoryId = "BeforeTutorialA";
            SceneManager.LoadScene("80. Title");
        }
    }
}
