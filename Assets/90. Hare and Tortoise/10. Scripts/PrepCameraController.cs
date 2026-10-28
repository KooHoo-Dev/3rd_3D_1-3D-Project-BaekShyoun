using Study.Utilities;
using UnityEngine;

namespace HareAndTortoise
{
    // 1. 스크롤을 올리면 확대, 내리면 축소   (position.y)
    // 2. 우클릭으로 카메라 위치 이동 (position.x, position.z)
    // -> 준비시간 동안엔 거북이 우클릭을 막아야만 정상적으로 먹힐 거임.
    // 3. 여유가 되면 카메라 이동에 제한을 걸어 맵 밖으로 볼 수 없게 할 것.
    
    public class PrepCameraController : MonoBehaviour
    {
        [SerializeField] private float zoomSpeed = 1.0f;
        [SerializeField] private float moveSpeed = 1.0f;

        private void Update()
        {
            float scroll = SimpleInput.GetMouseScrollDelta();
        }
    }
}