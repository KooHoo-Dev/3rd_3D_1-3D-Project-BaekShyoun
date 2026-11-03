using UnityEngine;

namespace HareAndTortoise
{
    // 타이틀 흑화 여부와 별개로, 각 경주의 클리어 기록만 저장한다.
    public static class StageProgress
    {
        private const string ClearKey = "HareAndTortoise.Clear.";

        public static bool IsCleared(StageType stage)
        {
            return PlayerPrefs.GetInt(ClearKey + stage, 0) == 1;
        }

        public static bool IsUnlocked(StageType stage)
        {
            switch (stage)
            {
                case StageType.TutorialA: return true;
                case StageType.TutorialB:
                    return IsCleared(StageType.TutorialA);
                case StageType.TutorialC:
                    return IsCleared(StageType.TutorialA) && IsCleared(StageType.TutorialB);
                case StageType.Stage1:
                    return IsCleared(StageType.TutorialA) && IsCleared(StageType.TutorialB) &&
                           IsCleared(StageType.TutorialC);
                default: return false;
            }
        }

        public static void MarkCleared(StageType stage)
        {
            PlayerPrefs.SetInt(ClearKey + stage, 1);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(ClearKey + StageType.TutorialA);
            PlayerPrefs.DeleteKey(ClearKey + StageType.TutorialB);
            PlayerPrefs.DeleteKey(ClearKey + StageType.TutorialC);
            PlayerPrefs.DeleteKey(ClearKey + StageType.Stage1);
            PlayerPrefs.Save();
        }
    }
}
