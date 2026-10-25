using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using Cursor = UnityEngine.Cursor;

namespace HareAndTortoise
{
    public class TortoiseController : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");

        [Header("Move Settings")]   
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float horizontalSensitivity = 1.0f;
        [SerializeField] private float verticalSensitivity = 1.0f;
        [SerializeField] private float lockOnSensitivity = 0.1f;
        
        [SerializeField] private float pitchAngleMin;
        [SerializeField] private float pitchAngleMax;

        [Header("Grenade Settings")] 
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private Transform grenadeHoldPoint;
        [SerializeField] private float throwSpeed = 12f;
        [SerializeField] private float throwUpwardSpeed = 5f;
        private GameObject heldGrenade;
        
        [Header("Trajectory Settings")]
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private int trajectoryPointCount = 60;
        [SerializeField] private float trajectoryTimeStep = 0.1f;
        
        [Header("Landing Circle Settings")]
        [SerializeField] private LineRenderer landingCircle;
        [SerializeField] private float landingCircleRadius = 0.5f;
        [SerializeField] private int landingCirclePointCount = 40;
        
        [Header("Ref")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float distance = 4f; // radius. 공전 궤도의 크기
        
        // 입력 회전량들을 모아둘겁니다
        private float angleX = 0.0f;
        private float angleY = 0.0f;
        
        // 자식 객체 animator
        private Animator animator;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            UpdatePosition();
            UpdateAngle();

            // 조준 및 발사
            AimGrenade();
        }

        private void LateUpdate()
        {
            UpdateCameraRotation();
            DrawTrajectory();
        }

        private void UpdateAngle()
        {
            Vector2 delta = SimpleInput.GetMouseDelta();
            // 우클릭 시(락온 시) 감도 떨구기
            angleX -= SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right)
                ? (delta.y * verticalSensitivity) * lockOnSensitivity : (delta.y * verticalSensitivity);
            angleY += SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right)
                ? (delta.x * horizontalSensitivity) * lockOnSensitivity : (delta.x * horizontalSensitivity);
            angleX = Mathf.Clamp(angleX, pitchAngleMin, pitchAngleMax);
        }
        
        private void UpdatePosition()
        {
            animator.SetFloat(Speed, 0f);
            if (RaceManager.Instance.canStartTortoise == false) return;
            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            if (inputAxis.sqrMagnitude < 0.00001f) return;

            animator.SetFloat(Speed, moveSpeed);
            // inputAxis를 3차원 입력 벡터로 바꿔줍니다.
            Vector3 inputVector = new Vector3(inputAxis.x, 0f, inputAxis.y);
            
            // angleY의 값을 기준으로 회전량을 생성해 줍니다.
            // pivot의 로컬 기준이 됩니다. 카메라의 좌우 각도가 담겨있음
            Quaternion yaw = Quaternion.Euler(0f, angleY, 0f);
            
            // inputVector를 yaw만큼 회전시킨 이동 벡터를 생성합니다.            
            Vector3 moveVector = (yaw * inputVector).normalized;
            // 내가 이번 프레임에 이동해야할 월드 벡터가 나옵니다.
            transform.position += moveVector * (moveSpeed * Time.deltaTime);

            // 몸과 루트의 회전적용합니다
            Quaternion targetRotation = Quaternion.LookRotation(moveVector);
            transform.rotation = targetRotation;
        }

        private void UpdateCameraRotation()
        {
            // 우클릭 시(락온 시) 카메라 거리 가깝게
            float targetDistance =
                SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right) ? 2f : 4f;

            distance = Mathf.Lerp(distance, targetDistance, 5f * Time.deltaTime);
            
            cameraPivot.rotation = Quaternion.Euler(angleX, angleY, 0f);
            cameraTransform.localPosition = new Vector3(1.5f, 0f, -distance);
        }
        
        private void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        # region 수류탄 로직
        
        // 수류탄 조준
        private void AimGrenade()
        {
            if (RaceManager.Instance.canStartTortoise == false) return;
            if (SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right) == false) return;
            if (SimpleInput.GetKeyDown(Key.F))
            {
                if (heldGrenade == null)
                {
                    heldGrenade = Instantiate(grenadePrefab, grenadeHoldPoint, false);

                    heldGrenade.transform.localPosition = Vector3.zero;
                    heldGrenade.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Destroy(heldGrenade);
                    heldGrenade = null;
                }
            }

            if (heldGrenade != null && SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left))
            {
                ThrowGrenade();
            }
        }
        
        // 수류탄 발사
        private void ThrowGrenade()
        {
            if (heldGrenade == null) return;
            GameObject grenade = heldGrenade;
            heldGrenade = null;

            grenade.transform.SetParent(null, true);
            
            Rigidbody rBody = grenade.GetComponent<Rigidbody>();

            rBody.isKinematic = false;

            foreach (Collider col in grenade.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
            }

            Vector3 direction = Quaternion.Euler(angleX, angleY, 0f) * Vector3.forward;
            Vector3 velocity = direction * throwSpeed + Vector3.up * throwUpwardSpeed;
            rBody.AddForce(velocity, ForceMode.VelocityChange);
        }
        
        # endregion
        
        # region 포물선 그리기
        private void DrawTrajectory()
        {
            landingCircle.positionCount = 0;
            bool isAiming =
                SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right);

            if (RaceManager.Instance.canStartTortoise == false || isAiming == false || heldGrenade == null)
            {
                trajectoryLine.positionCount = 0;
                return;
            }

            Vector3 startPosition = heldGrenade.transform.position;

            Vector3 direction =
                Quaternion.Euler(angleX, angleY, 0f) * Vector3.forward;

            Vector3 velocity =
                direction * throwSpeed + Vector3.up * throwUpwardSpeed;

            trajectoryLine.positionCount = trajectoryPointCount;
            trajectoryLine.SetPosition(0, startPosition);

            Vector3 previousPoint = startPosition;

            for (int i = 1; i < trajectoryPointCount; i++)
            {
                float time = i * trajectoryTimeStep;

                Vector3 point =
                    startPosition +
                    velocity * time +
                    Physics.gravity * (0.5f * time * time);

                // 이전 점과 현재 점 사이에 바닥이 있으면 선을 끝내기
                Vector3 segment = point - previousPoint;

                if (Physics.Raycast(
                        previousPoint,
                        segment.normalized,
                        out RaycastHit hit,
                        segment.magnitude,
                        LayerMask.GetMask("Ground"),
                        QueryTriggerInteraction.Ignore))
                {
                    trajectoryLine.SetPosition(i, hit.point);
                    trajectoryLine.positionCount = i + 1;
                    
                    DrawLandingCircle(hit.point);
                    break;
                }

                trajectoryLine.SetPosition(i, point);
                previousPoint = point;
            }
        }
        
        private void DrawLandingCircle(Vector3 center)
        {
            landingCircle.positionCount = landingCirclePointCount;

            for (int i = 0; i < landingCirclePointCount; i++)
            {
                float angle = i * Mathf.PI * 2f / landingCirclePointCount;

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)) * landingCircleRadius;

                // 바닥과 겹쳐 깜빡이지 않도록 조금 위에 표시
                Vector3 point = center + offset + Vector3.up * 0.03f;

                landingCircle.SetPosition(i, point);
            }
        }
        # endregion
        
        
    }
}

