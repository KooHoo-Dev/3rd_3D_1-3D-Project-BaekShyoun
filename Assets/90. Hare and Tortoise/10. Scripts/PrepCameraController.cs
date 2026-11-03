using JetBrains.Annotations;
using Study.Utilities;
using UnityEngine;
using Unity.Cinemachine;

namespace HareAndTortoise
{
    // 1. 스크롤을 올리면 확대, 내리면 축소   (position.y)
    // 2. 우클릭으로 카메라 위치 이동 (position.x, position.z)
    // -> 준비시간 동안엔 거북이 우클릭을 막아야만 정상적으로 먹힐 거임.
    // 3. 여유가 되면 카메라 이동에 제한을 걸어 맵 밖으로 볼 수 없게 할 것.
    
    public class PrepCameraController : MonoBehaviour
    {
        [Header("MoveSettings")]
        [SerializeField] private float zoomSpeed = 1.0f;
        [SerializeField] private float moveSpeed = 0.1f;
        [SerializeField] private float minSize = 10f;
        [SerializeField] private float maxSize = 60f;

        [Header("MapSettings")]
        [SerializeField] private Terrain terrain;
        [SerializeField] private GameObject landminePrefab;
        private Vector2 mapMin;
        private Vector2 mapMax;

        private CinemachineCamera prepCamera;

        private void Awake()
        {
            prepCamera = GetComponent<CinemachineCamera>();

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            mapMin = new Vector2(origin.x, origin.z);
            mapMax = new Vector2(origin.x + size.x, origin.z + size.z);
        }
        
        private void Update()
        {
            if (RaceManager.Instance.IsPreParing == false) return;
            UpdateZoom();
            UpdateCameraMovement();
            MapPosition();
            PlaceLandmine();
        }

        private void PlaceLandmine()
        {
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left) == false) return;
            
            Vector2 mousePosition = SimpleInput.GetMousePosition();

            float mouseX = mousePosition.x / Screen.width;
            float mouseY = mousePosition.y / Screen.height;

            float halfHeight = prepCamera.Lens.OrthographicSize;
            float halfWidth = halfHeight * ((float)Screen.width / Screen.height);

            Vector3 position = transform.position;
            position.x += (mouseX - 0.5f) * halfWidth * 2f;
            position.z += (mouseY - 0.5f) * halfHeight * 2f;

            position.y = terrain.SampleHeight(position) + terrain.GetPosition().y;

            if (RaceManager.Instance.TryUseLandmine() == false) return;
            Instantiate(landminePrefab, position, Quaternion.identity);
        }

        private void UpdateZoom()
        {
            float scroll = SimpleInput.GetMouseScrollDelta();

            float size = prepCamera.Lens.OrthographicSize;
            size -= scroll * zoomSpeed;

            float zoomMax = GetMaxZoomSize();
            float zoomMin = Mathf.Min(minSize, zoomMax);

            prepCamera.Lens.OrthographicSize = Mathf.Clamp(size, zoomMin, zoomMax);
        }

        private float GetMaxZoomSize()
        {
            float aspect = (float)Screen.width / Screen.height;

            float mapWidth = mapMax.x - mapMin.x;
            float mapHeight = mapMax.y - mapMin.y;

            float terrainMaxSize = Mathf.Min(mapHeight * 0.5f, mapWidth / (2f * aspect));

            return Mathf.Min(maxSize, terrainMaxSize);
        }
        
        private void UpdateCameraMovement()
        {
            if (SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right) == false) return;
            
            Vector2 delta = SimpleInput.GetMouseDelta();

            Vector3 position = transform.position;
            position.x -= delta.x * moveSpeed;
            position.z -= delta.y * moveSpeed;

            transform.position = position;
        }


        private void MapPosition()
        {
            float aspect = (float)Screen.width / Screen.height;
            float halfHeight = prepCamera.Lens.OrthographicSize;
            float halfWidth = halfHeight * aspect;

            Vector3 position = transform.position;

            position.x = Mathf.Clamp(position.x, mapMin.x + halfWidth, mapMax.x - halfWidth);
            position.z = Mathf.Clamp(position.z, mapMin.y + halfHeight, mapMax.y - halfHeight);

            transform.position = position;
        }
    }
}
