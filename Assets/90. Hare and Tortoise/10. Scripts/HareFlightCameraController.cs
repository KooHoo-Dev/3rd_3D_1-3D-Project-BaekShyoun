using Unity.Cinemachine;
using UnityEngine;

namespace HareAndTortoise
{
    public class HareFlightCameraController : MonoBehaviour
    {
        [SerializeField] private HareController hare;

        private CinemachineCamera flightCamera;

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