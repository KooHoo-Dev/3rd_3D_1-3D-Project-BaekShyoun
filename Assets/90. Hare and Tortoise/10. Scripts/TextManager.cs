using Jay;
using TMPro;
using System.Threading;
using Cysharp.Threading.Tasks;

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

        // 지정한 시간 동안 표시한다. 취소되면 다음 자막을 건드리지 않고 종료한다.
        public static async UniTask SetMessageAsync(TMP_Text text, string message,
            float duration = 3f, CancellationToken cancellationToken = default)
        {
            SetMessage(text, true, message);
            await UniTask.WaitForSeconds(duration, cancellationToken: cancellationToken);
            SetMessage(text, false);
        }
    }
}
