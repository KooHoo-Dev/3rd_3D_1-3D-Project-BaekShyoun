using UnityEngine;
using UnityEngine.SceneManagement;

namespace HareAndTortoise
{
    public class TitleController : MonoBehaviour
    {
        [SerializeField] private bool isVillain;

        private void Start()
        {
            isVillain = SceneController.IsVillain;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StartGame()
        {
            if (isVillain) SceneManager.LoadScene("02. Tutorial C");
            else SceneController.PlayStory("BeforeTutorialA");
        }

        [ContextMenu("상태 초기화")]
        public void ResetProgress()
        {
            if (Application.isPlaying == false) return;
            SceneController.ResetProgress();
        }
    }
}
