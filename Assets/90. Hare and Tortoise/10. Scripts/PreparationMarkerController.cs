using UnityEngine;

namespace HareAndTortoise
{
    // 토끼 이름표는 항상 표시하고, 거북이 위치 표식은 준비시간에만 표시한다.
    public class PreparationMarkerController : MonoBehaviour
    {
        [SerializeField] private RectTransform hareMarker;
        [SerializeField] private RectTransform tortoiseMarker;
        [SerializeField] private float worldHeight = 1.5f;
        [SerializeField] private float hareHeightAboveHips = 0.7f;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 32f);

        private RectTransform markerArea;
        private Canvas canvas;
        private Camera worldCamera;
        private RaceManager race;
        private Transform hare;
        private Transform tortoise;
        private float hareHeight;

        private void OnEnable()
        {
            // Cinemachine이 카메라를 옮긴 뒤, UI 렌더링 직전에 위치를 계산한다.
            Canvas.willRenderCanvases += UpdateMarkers;
        }

        private void Start()
        {
            markerArea = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            worldCamera = Camera.main;
            race = RaceManager.Instance;
            HareController hareController = FindFirstObjectByType<HareController>();
            TortoiseController tortoiseController = FindFirstObjectByType<TortoiseController>();
            // 래그돌이 루트에서 멀리 날아가도 이름표는 실제 몸을 따라간다.
            hare = hareController != null ? hareController.FlightCameraTarget : null;
            hareHeight = hareController != null && hare != hareController.transform
                ? hareHeightAboveHips : worldHeight;
            tortoise = tortoiseController != null ? tortoiseController.transform : null;
        }

        private void UpdateMarkers()
        {
            bool canShow = race != null && worldCamera != null && canvas != null;
            UpdateMarker(hareMarker, hare, canShow, hareHeight);
            UpdateMarker(tortoiseMarker, tortoise,
                canShow && race.HasPreparation && race.IsPreParing && race.IsRaceFinished == false,
                worldHeight);
        }

        private void UpdateMarker(RectTransform marker, Transform target, bool show, float height)
        {
            if (marker == null) return;
            if (show == false || target == null || markerArea == null)
            {
                if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
                return;
            }

            // 화면 UI의 위치만 옮긴다. 회전·크기는 건드리지 않으며 장애물 가림 검사도 하지 않는다.
            Vector3 position = target.position + Vector3.up * height;
            Vector3 viewport = worldCamera.WorldToViewportPoint(position);
            bool visible = viewport.z > worldCamera.nearClipPlane &&
                           viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
            if (visible == false) return;

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screen = worldCamera.WorldToScreenPoint(position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(markerArea, screen, uiCamera,
                    out Vector2 localPosition))
            {
                localPosition += screenOffset;
                // 캐릭터가 화면 가장자리에 있어도 글자가 잘리지 않도록 표식만 안쪽으로 제한한다.
                Rect rect = markerArea.rect;
                Vector2 halfSize = marker.rect.size * 0.5f;
                localPosition.x = Mathf.Clamp(localPosition.x, rect.xMin + halfSize.x, rect.xMax - halfSize.x);
                localPosition.y = Mathf.Clamp(localPosition.y, rect.yMin + halfSize.y, rect.yMax - halfSize.y);
                marker.anchoredPosition = localPosition;
            }
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= UpdateMarkers;
        }
    }
}
