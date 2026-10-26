using Jay;
using TMPro;

namespace HareAndTortoise
{
    // Text 관련 메서드
    public class TextManager : SingletonBase<TextManager>
    {
        public static void SetMessage(TMP_Text text, bool isActive, string message = "")
        {
            text.gameObject.SetActive(isActive);
            text.SetText(message);
        }
    }
}