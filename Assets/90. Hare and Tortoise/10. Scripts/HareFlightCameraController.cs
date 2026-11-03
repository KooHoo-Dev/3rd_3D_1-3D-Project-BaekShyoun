using Unity.Cinemachine;
using UnityEngine;

namespace HareAndTortoise
{
    public class HareFlightCameraController : MonoBehaviour
    {
        [SerializeField] private HareController hare;

        private CinemachineCamera flightCamera;

        // 토끼 시점으로 전환되는 순간부터, 거북이 시점으로 돌아가는 블렌드가 끝날 때까지.
        public bool IsShowingHare => isActiveAndEnabled && flightCamera != null &&
            flightCamera.isActiveAndEnabled && hare != null &&
            (hare.IsInFlight || CinemachineCore.IsLive(flightCamera));

        private void Start()
        {
            flightCamera = GetComponent<CinemachineCamera>();

            flightCamera.Follow = hare.FlightCameraTarget;
            flightCamera.LookAt = hare.FlightCameraTarget;
            flightCamera.Priority = 0;
        }
        
        private void Update()
        {
            // 공중 비행 중에만 기존 카메라보다 우선한다.
            flightCamera.Priority = hare.IsInFlight ? 30 : 0;
        }

    }
}
