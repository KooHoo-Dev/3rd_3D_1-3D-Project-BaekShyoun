using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Cursor = UnityEngine.Cursor;

namespace HareAndTortoise
{
    public class TortoiseController : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Throw = Animator.StringToHash("Throw");
        private static readonly int Aiming = Animator.StringToHash("IsAiming");

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
        [SerializeField] private float throwSpeed = 16f;
        private GameObject heldGrenade;
        private bool isThrowing;
        public bool IsMoving { get; private set; }
        
        [Header("Trajectory Settings")]
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private int trajectoryPointCount = 60;
        [SerializeField] private float trajectoryTimeStep = 0.1f;

        [Header("Aim Settings")]
        [SerializeField] private float aimSensitivity = 0.18f;
        [SerializeField] private float aimPitchMin = -35f;
        [SerializeField] private float aimPitchMax = 65f;
        [SerializeField] private float throwArcAngle = 10f;
        [SerializeField] private float aimCameraDistance = 2.2f;
        [SerializeField] private float aimCameraHeight = 1.4f;
        [SerializeField] private float aimShoulderOffset = 0.65f;
        [SerializeField] private float aimFieldOfView = 45f;
        [SerializeField] private float aimCameraBlendSpeed = 10f;
        [SerializeField] private float grenadePreviewRadius = 0.05f;
        private Vector3 defaultPivotLocalPosition;
        private CinemachineCamera playCamera;
        private float defaultFieldOfView;
        private readonly RaycastHit[] worldHits = new RaycastHit[32];

        private Vector3 aimTarget;
        private bool isAimValid;
        private float previewFlightTime;
        private Vector3 pendingAimTarget;
        private float pendingFlightTime;

        public bool IsAiming => 
            RaceManager.Instance.CanAttack && RaceManager.Instance.canStartTortoise && heldGrenade != null
            && SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right);
        
        [Header("Landing Circle Settings")]
        [SerializeField] private LineRenderer landingCircle;
        [SerializeField] private float landingCircleRadius = 0.5f;
        [SerializeField] private int landingCirclePointCount = 40;
        
        [Header("Ref")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float distance = 4f; // radius. 공전 궤도의 크기

        [Header("Stage1 Route Settings")]
        [SerializeField] private Transform outlineWaypoints;
        [SerializeField] private float checkpointRadius = 2f;
        [SerializeField] private int waypointIndex;
        public bool HasDoneRoute => outlineWaypoints != null && outlineWaypoints.childCount > 0
                                                             && waypointIndex >= outlineWaypoints.childCount;
        
        
        // 입력 회전량들을 모아둘겁니다
        private float angleX = 0.0f;
        private float angleY = 0.0f;
        
        // 자식 객체 animator
        private Animator animator;
        private HareFlightCameraController hareFlightCamera;

        private void Awake()
        {
            defaultPivotLocalPosition = cameraPivot.localPosition;
            playCamera = cameraTransform.GetComponent<CinemachineCamera>();
            if (playCamera != null) defaultFieldOfView = playCamera.Lens.FieldOfView;
            animator = GetComponentInChildren<Animator>();
            hareFlightCamera = FindFirstObjectByType<HareFlightCameraController>();
        }

        private void Update()
        {
            LockCursor();
            UpdatePosition();
            UpdateAngle();

            // 조준 중에는 이동 방향과 관계없이 상체가 겨누는 방향을 향한다.
            if (IsAiming || isThrowing) transform.rotation = Quaternion.Euler(0f, angleY, 0f);

            // 조준 및 발사
            AimGrenade();

            // 공격 가능한 씬에서만 조준 상태에 맞춰 상체 준비 자세를 유지한다.
            if (RaceManager.Instance.CanAttack)
                animator.SetBool(Aiming, IsAiming && isThrowing == false);

            if (RaceManager.Instance.IsStage1 && RaceManager.Instance.canStartTortoise)
                CheckStageRoute();
        }

        private void LateUpdate()
        {
            UpdateCameraRotation();
            DrawTrajectory();
        }
        
        # region Stage1 전용 메서드

        public void PlaceAtSpawn(Transform spawnPoint)
        {
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            angleY=spawnPoint.eulerAngles.y;
            cameraPivot.rotation = Quaternion.Euler(angleX, angleY, 0f);
        }

        private void CheckStageRoute()
        {
            if (HasDoneRoute) return;
            Transform target = outlineWaypoints.GetChild(waypointIndex);
            Vector3 offset = transform.position - target.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= checkpointRadius * checkpointRadius)
            {
                waypointIndex++;
            }
        }
        
        # endregion

        private void UpdateAngle()
        {
            if (RaceManager.Instance.canStartTortoise == false) return;
            if (isThrowing) return;
            Vector2 delta = SimpleInput.GetMouseDelta();
            if (IsAiming)
            {
                angleX = Mathf.Clamp(angleX - delta.y * aimSensitivity, aimPitchMin, aimPitchMax);
                angleY += delta.x * aimSensitivity;
                return;
            }
            // 우클릭 시(락온 시) 감도 떨구기
            angleX -= SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right)
                ? (delta.y * verticalSensitivity) * lockOnSensitivity : (delta.y * verticalSensitivity);
            angleY += SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right)
                ? (delta.x * horizontalSensitivity) * lockOnSensitivity : (delta.x * horizontalSensitivity);
            angleX = Mathf.Clamp(angleX, pitchAngleMin, pitchAngleMax);
        }

        private void UpdateAimTarget()
        {
            isAimValid = false;
            if (IsAiming == false || isThrowing) return;
            isAimValid = TraceTrajectory(heldGrenade.transform.position, CalculateAimVelocity(), false);
        }
        
        private void UpdatePosition()
        {
            IsMoving = false;
            animator.SetFloat(Speed, 0f);
            if (RaceManager.Instance.canStartTortoise == false) return;
            // 토끼를 비추는 동안 화면 밖에서 거북이가 이동하지 않도록 막는다.
            if (hareFlightCamera != null && hareFlightCamera.IsShowingHare) return;
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
            IsMoving = moveSpeed > 0f;

            // 몸과 루트의 회전적용합니다
            Quaternion targetRotation = Quaternion.LookRotation(moveVector);
            transform.rotation = targetRotation;
        }

        private void UpdateCameraRotation()
        {
            if (RaceManager.Instance.canStartTortoise == false) return;

            bool aiming = IsAiming || isThrowing;
            float blend = 1f - Mathf.Exp(-(aiming ? aimCameraBlendSpeed : 5f) * Time.deltaTime);
            Vector3 focus = aiming
                ? transform.position + Vector3.up * aimCameraHeight
                : transform.TransformPoint(defaultPivotLocalPosition);
            cameraPivot.position = Vector3.Lerp(cameraPivot.position, focus, blend);
            // 조준 회전은 마우스와 즉시 일치시키고 위치·확대만 부드럽게 전환한다.
            cameraPivot.rotation = Quaternion.Euler(angleX, angleY, 0f);
            distance = Mathf.Lerp(distance, aiming ? aimCameraDistance : 4f, blend);
            Vector3 cameraPosition = new Vector3(aiming ? aimShoulderOffset : 1.5f, 0f, -distance);
            Vector3 localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraPosition, blend);
            Vector3 desiredPosition = cameraPivot.TransformPoint(localPosition);
            if (aiming && TryHitWorld(cameraPivot.position, desiredPosition, 0.2f, out RaycastHit cameraHit))
                desiredPosition = cameraHit.point + cameraHit.normal * 0.22f;
            cameraTransform.position = desiredPosition;

            if (playCamera != null)
            {
                LensSettings lens = playCamera.Lens;
                lens.FieldOfView = Mathf.Lerp(lens.FieldOfView,
                    aiming ? aimFieldOfView : defaultFieldOfView, blend);
                playCamera.Lens = lens;
            }
        }
        
        private void LockCursor()
        {
            bool showCursor = RaceManager.Instance.IsPreParing ||
                              RaceManager.Instance.IsRaceFinished;

            Cursor.visible = showCursor;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        }

        # region 수류탄 로직
        
        // 수류탄 조준
        private void AimGrenade()
        {
            if (isThrowing) return;
            
            if (RaceManager.Instance.CanAttack == false) return;
            if (RaceManager.Instance.canStartTortoise == false) return;
            if (SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right) == false) return;
            if (SimpleInput.GetKeyDown(Key.F))
            {
                if (heldGrenade == null)
                {
                    if (RaceManager.Instance.GrenadeCount <= 0)
                    {
                        SoundManager.Play(SoundEffect.ItemUnavailable);
                        return;
                    }
                    heldGrenade = Instantiate(grenadePrefab, grenadeHoldPoint, false);
                    SoundManager.Play(SoundEffect.GrenadeEquip);

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
                // 클릭한 프레임의 방향으로 한 번 더 판정하고, 평소 미리보기는 LateUpdate에서만 계산한다.
                UpdateAimTarget();
                ThrowGrenade();
            }
        }

        private Vector3 CalculateAimVelocity()
        {
            // 카메라의 상하 각도에 약간의 투척 각도를 더하고, 던지는 힘은 일정하게 유지한다.
            float pitch = Mathf.Clamp(angleX, aimPitchMin, aimPitchMax) - throwArcAngle;
            return Quaternion.Euler(pitch, angleY, 0f) * Vector3.forward * throwSpeed;
        }
        
        // 수류탄 발사
        private void ThrowGrenade()
        {
            if (isThrowing || heldGrenade == null || isAimValid == false) return;
            if (RaceManager.Instance.TryUseGrenade() == false) return;

            // 손을 놓기 전까지 움직이므로 클릭 시점의 착지 위치와 도달 시간을 보관한다.
            pendingAimTarget = aimTarget;
            pendingFlightTime = Mathf.Max(0.05f, previewFlightTime);
            isThrowing = true;
            animator.SetTrigger(Throw);
        }
        
        public void ReleaseGrenade()
        {
            if (isThrowing == false || heldGrenade == null) return;

            GameObject grenade = heldGrenade;
            heldGrenade = null;

            grenade.transform.SetParent(null, true);

            Rigidbody rBody = grenade.GetComponent<Rigidbody>();
            rBody.isKinematic = false;

            Collider[] colliders = grenade.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = true;
            }

            // 실제 손 위치에서 출발하되, 클릭할 때 표시한 지점에 같은 시간으로 도착한다.
            Vector3 velocity = (pendingAimTarget - grenade.transform.position) / pendingFlightTime
                - Physics.gravity * (0.5f * pendingFlightTime);
            rBody.linearVelocity = velocity;
            SoundManager.Play(SoundEffect.GrenadeThrow);

            isThrowing = false;
        }
        
        # endregion
        
        # region 포물선 그리기
        private void DrawTrajectory()
        {
            if (RaceManager.Instance.CanAttack == false) return;
            trajectoryLine.positionCount = 0;
            landingCircle.positionCount = 0;
            if (IsAiming == false || isThrowing) return;
            isAimValid = TraceTrajectory(heldGrenade.transform.position, CalculateAimVelocity(), true);
        }

        // 미리보기와 클릭 판정에서 같은 계산을 사용하고, 첫 충돌 지점까지만 표시한다.
        private bool TraceTrajectory(Vector3 startPosition, Vector3 velocity, bool draw)
        {
            int count = Mathf.Max(2, trajectoryPointCount);
            float step = Mathf.Max(0.01f, trajectoryTimeStep);
            if (draw) trajectoryLine.positionCount = count;
            if (draw) trajectoryLine.SetPosition(0, startPosition);

            Vector3 previousPoint = startPosition;

            for (int i = 1; i < count; i++)
            {
                float time = i * step;

                Vector3 point =
                    startPosition +
                    velocity * time +
                    Physics.gravity * (0.5f * time * time);

                Vector3 segment = point - previousPoint;
                if (TryHitWorld(previousPoint, point, grenadePreviewRadius, out RaycastHit hit))
                {
                    // 충돌 순간 수류탄의 중심 위치를 보관해 발사 계산에 사용한다.
                    float fraction = segment.magnitude > 0.0001f
                        ? Mathf.Clamp01(hit.distance / segment.magnitude) : 0f;
                    aimTarget = Vector3.Lerp(previousPoint, point, fraction);
                    previewFlightTime = (i - 1 + fraction) * step;
                    if (draw)
                    {
                        trajectoryLine.SetPosition(i, aimTarget);
                        trajectoryLine.positionCount = i + 1;
                        if (hit.normal.y > 0.5f) DrawLandingCircle(hit.point);
                    }
                    return true;
                }

                if (draw) trajectoryLine.SetPosition(i, point);
                previousPoint = point;
            }
            return false;
        }

        private bool TryHitWorld(Vector3 from, Vector3 to, float radius, out RaycastHit nearest)
        {
            nearest = default;
            Vector3 segment = to - from;
            float length = segment.magnitude;
            if (length < 0.0001f) return false;
            int count = Physics.SphereCastNonAlloc(from, Mathf.Max(0.01f, radius), segment / length,
                worldHits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (worldHits[i].collider.transform.IsChildOf(transform)) continue;
                if (worldHits[i].distance >= nearestDistance) continue;
                nearest = worldHits[i];
                nearestDistance = nearest.distance;
                found = true;
            }
            return found;
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

