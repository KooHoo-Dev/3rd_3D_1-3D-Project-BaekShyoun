using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HareAndTortoise
{
    public class TitleController : MonoBehaviour
    {
        [SerializeField] private bool isVillain;
        [Header("Title Appearance")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite normalBackground;
        [SerializeField] private Sprite darkBackground;

        private void Start()
        {
            isVillain = SceneController.IsVillain;
            // 제목까지 포함된 PNG를 진행 상태에 맞춰 교체한다.
            if (backgroundImage != null)
                backgroundImage.sprite = isVillain ? darkBackground : normalBackground;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StartGame()
        {
            SceneManager.LoadScene("70. Stage");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        [ContextMenu("상태 초기화")]
        public void ResetProgress()
        {
            if (Application.isPlaying == false) return;
            SceneController.ResetProgress();
        }
    }
}
