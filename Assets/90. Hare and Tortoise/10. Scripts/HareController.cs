using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;

namespace HareAndTortoise
{
    // 토끼의 경주 이동, 수면, 피격, 래그돌 전환과 코스 복귀
    public class HareController : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsSleeping = Animator.StringToHash("IsSleeping");
        private static readonly int StandingUp = Animator.StringToHash("Base Layer.Standing Up");
        private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");

        private CancellationToken destroyToken;

        [Header("Level Design")] 
        [SerializeField] private float moveSpeed = 3.5f;
        
        [Header("Goal Settings")]
        [SerializeField] private Transform goalPoint; // 토끼의 목표 지점

        [Header("Stage1 Route Settings")] [SerializeField]
        private Transform inlineWaypoints; // 인라인 경유지점
        private int waypointIndex; // 앞으로 향할 경유지점 번호

        public bool HasDoneRoute => inlineWaypoints != null && inlineWaypoints.childCount > 0 &&
                                    waypointIndex >= inlineWaypoints.childCount;

        // 마지막 경유지로 향하는 도중 날아가 결승선을 넘어도 골인을 인정한다.
        public bool CanFinishInFlight => IsInFlight && inlineWaypoints != null &&
            inlineWaypoints.childCount > 0 && waypointIndex >= inlineWaypoints.childCount - 1;

        [Header("Sleep Settings")]
        [SerializeField] private Transform sleepTree; // 토끼가 수면을 취할 나무 위치
        [SerializeField] private Transform sleepSpot; // 토끼가 실제로 잠드는 위치
        [SerializeField] private float sleepRadius = 3f; // 토끼가 자러 가기 위한 나무 중심의 반경
        private Vector3 prevPos; // 수면, 피격으로 코스를 벗어난 후 돌아갈 위치
        private bool hasSlept; // 한 번 잠든 후 나무에서 무한으로 잠드는 것 방지
        private HareState state = HareState.ToGoal; // 토끼의 현재 행동 상태

        [Header("Knockback Settings")]
        [SerializeField] private float horizontalImpulse = 8f; // 수평 넉백
        [SerializeField] private float upwardImpulse = 5f; // 수직(위) 넉백
        [SerializeField] private float attackedDuration = 10f; // 피격 후 복귀를 위해 기다리는 시간
        [SerializeField] private float spinImpulse = 15f; // 공중제비를 위한 회전 

        [Header("Ragdoll Settings")]
        [SerializeField] private Rigidbody ragdollHips; // 골반 Rigidbody
        public Transform FlightCameraTarget => ragdollHips != null ? ragdollHips.transform : transform;
        public bool IsInFlight { get; private set; }
        // 공중 비행이나 기상 중에는 발소리가 나오지 않도록 실제 경로 이동만 확인한다.
        public bool IsMoving => RaceManager.Instance != null && RaceManager.Instance.canStartHare &&
            (state == HareState.ToGoal || state == HareState.ToSleep || state == HareState.Returning) &&
            agent != null && agent.enabled && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.01f;
        private Rigidbody[] ragdollBodies; // 골반의 자식객체에 있는 Rigidbody
        private Vector3[] ragdollLocalPositions; // 각 뼈의 위치
        private Quaternion[] ragdollLocalRotations; // 각 뼈의 회전


        private enum HareState
        {
            ToGoal,     // 결승점 이동
            ToSleep,    // 자러가는 중
            Sleeping,   // 자는 중
            Attacked,   // 피격된 상태
            GettingUp,  // 기상 모션 중에는 이동과 추가 피격을 막는다.
            Returning,  // 복귀 중
        }

        // 컴포넌트
        private NavMeshAgent agent; // 경로 탐색
        private Animator animator; // 애니메이션
        private Rigidbody rootBody; // Hare Rigidbody 저장용
        private Collider rootCollider; // Hare Collider. 래그돌 사용 시 끈다.
        
        private void Awake()
        {
            destroyToken = this.GetCancellationTokenOnDestroy();
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            rootBody = GetComponent<Rigidbody>();
            rootCollider = GetComponent<Collider>();
            hasSlept = false;

            ragdollBodies = ragdollHips != null 
                ? ragdollHips.GetComponentsInChildren<Rigidbody>(true) : new Rigidbody[0];
            ragdollLocalPositions = new Vector3[ragdollBodies.Length];
            ragdollLocalRotations = new Quaternion[ragdollBodies.Length];

            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                Transform bone = ragdollBodies[i].transform;

                ragdollLocalPositions[i] = bone.localPosition;
                ragdollLocalRotations[i] = bone.localRotation;
                
                if (bone.GetComponent<HareFlightCollision>() == null)
                {
                    bone.gameObject.AddComponent<HareFlightCollision>();
                }

            }
            
            if (ragdollBodies.Length == 0 && GetComponent<HareFlightCollision>() == null)
            {
                gameObject.AddComponent<HareFlightCollision>();
            }
            
            SetRagdollActive(false);
        }
        
        private void Update()
        {
            if (RaceManager.Instance.canStartHare == false)
            {
                agent.speed = 0f;
                animator.SetFloat(Speed, agent.speed);
                return;
            }
            CheckState();
        }
        
        # region Ragdoll Methods
        
        private void SetRagdollActive(bool isActive)
        {
            if (ragdollBodies.Length == 0) return;
            
            animator.enabled = isActive == false;
            if (rootCollider != null)
            {
                rootCollider.enabled = isActive == false;
            }

            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                Rigidbody rBody = ragdollBodies[i];

                // 빠르게 날아갈 때는 연속 충돌 검출, 비활성 상태에서는 일반 검출을 사용한다.
                if (isActive == false)
                    rBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rBody.isKinematic = isActive == false;
                if (isActive)
                    rBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rBody.detectCollisions = isActive;
            }
        }

        private void ResetRagdollPose()
        {
            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                Transform bone = ragdollBodies[i].transform;

                bone.localPosition = ragdollLocalPositions[i];
                bone.localRotation = ragdollLocalRotations[i];
            }
        }

        private bool IsRagdollSleeping()
        {
            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                if (ragdollBodies[i].IsSleeping() == false) return false;
            }

            return true;
        }

        private async UniTaskVoid RecoverFromRagdoll()
        {
            await UniTask.WaitForSeconds(attackedDuration, cancellationToken: destroyToken);
            float time = 0f;
            while (IsRagdollSleeping() == false && time < 3f)
            {
                await UniTask.NextFrame(cancellationToken: destroyToken);
                time += Time.deltaTime;
            }

            Vector3 recoveryPosition = ragdollHips.position;
            bool foundPosition 
                = NavMesh.SamplePosition(recoveryPosition, out NavMeshHit hit, 3f, agent.areaMask);

            // 수관 위라서 지면을 찾지 못하면, 나무를 관통해 옮기지 않고 물리 상태로 빠져나온다.
            if (IsSupportedByTree())
            {
                await SlideOffTree();
                recoveryPosition = ragdollHips.position;
                foundPosition = NavMesh.SamplePosition(recoveryPosition, out hit, 3f, agent.areaMask);
            }

            if (foundPosition == false && RaceManager.Instance.IsStage1 == false) 
            { 
                foundPosition = NavMesh.SamplePosition(prevPos, out hit, 3f, agent.areaMask);
            }
            
            // 탐색을 실패했을 때의 임시 방편임. 나중에 수정될 것.
            if (foundPosition == false)
            {
                Debug.LogWarning("토끼가 복귀할 NavMesh를 찾지 못했습니다.");
                return;
            }
            
            state = HareState.GettingUp;
            IsInFlight = false;
            SetRagdollActive(false);
            ResetRagdollPose();

            transform.position = hit.position;
            animator.SetBool(IsSleeping, false);
            animator.SetFloat(Speed, 0f);

            // NavMeshAgent를 끈 상태에서 클립을 끝까지 재생한다.
            animator.Play(StandingUp, 0, 0f);
            animator.Update(0f);
            SoundManager.Play(SoundEffect.HareGetUp);
            while (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == StandingUp &&
                   animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                await UniTask.NextFrame(cancellationToken: destroyToken);
            }

            animator.Play(Idle, 0, 0f);
            agent.enabled = true;
            if (agent.Warp(hit.position) == false) return;
            agent.ResetPath();
            SetReturnStat();
        }

        private bool IsSupportedByTree()
        {
            RaycastHit[] hits = Physics.RaycastAll(ragdollHips.position + Vector3.up * 0.3f,
                Vector3.down, 3f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.gameObject.name == "Tree Collision") return true;
            }
            return false;
        }

        private async UniTask SlideOffTree()
        {
            float elapsed = 0f;
            while (elapsed < 6f && IsSupportedByTree())
            {
                Vector3 direction = prevPos - ragdollHips.position;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f) direction = -transform.forward;
                direction.Normalize();
                for (int i = 0; i < ragdollBodies.Length; i++)
                {
                    Rigidbody body = ragdollBodies[i];
                    Vector3 velocity = body.linearVelocity;
                    velocity.x = direction.x * 2.5f;
                    velocity.z = direction.z * 2.5f;
                    body.linearVelocity = velocity;
                    body.WakeUp();
                }
                await UniTask.WaitForFixedUpdate(cancellationToken: destroyToken);
                elapsed += Time.fixedDeltaTime;
            }

            // 수관을 벗어난 뒤에도 낙하와 착지는 물리로 처리한다.
            elapsed = 0f;
            while (IsRagdollSleeping() == false && elapsed < 3f)
            {
                await UniTask.WaitForFixedUpdate(cancellationToken: destroyToken);
                elapsed += Time.fixedDeltaTime;
            }
        }
        
        # endregion
        
        # region stage 전용 메서드

        public void PlaceAtSpawn(Transform spawnPoint)
        {
            if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 1f, agent.areaMask) == false)
                return;
            if (agent.Warp(hit.position) == false) return;
            transform.rotation = spawnPoint.rotation;
        }

        private void FollowStageRoute()
        {
            SetHareMoving(moveSpeed);
            if (agent.pathPending) return;
            Transform target = waypointIndex < inlineWaypoints.childCount
                ? inlineWaypoints.GetChild(waypointIndex)
                : goalPoint;
            Vector3 offset = transform.position - target.position;
            offset.y = 0f;
            if (waypointIndex < inlineWaypoints.childCount && offset.sqrMagnitude <= 0.25f * 0.25f)
            {
                waypointIndex++;
                return;
            }

            if (agent.hasPath == false || (agent.destination - target.position).sqrMagnitude > 0.01f)
                agent.SetDestination(target.position);
        }
        
        # endregion

        private void CheckState()
        {
            switch (state)
            {
                case HareState.ToGoal:
                    MoveToGoal();
                    break;
                case HareState.ToSleep:
                    MoveToSleep();
                    break;
                case HareState.Sleeping:
                    break;
                case HareState.Attacked:
                    break;
                case HareState.GettingUp:
                    break;
                case HareState.Returning:
                    ReturnToTrack();
                    break;
            }
        }
        
        private void MoveToGoal()
        {
            if(RaceManager.Instance.IsStage1)
            {
                FollowStageRoute();
                return;
            }
            SetHareMoving(moveSpeed);
            if (agent.pathPending) return;
            if (agent.hasPath == false || (agent.destination - goalPoint.position).sqrMagnitude > 0.01f)
            {
                agent.SetDestination(goalPoint.position);
            }
            
            if (RaceManager.Instance.CanSleep == false) return;
            // 원형 범위 검사
            Vector3 offset = transform.position - sleepTree.position;
            offset.y = 0;

            if (hasSlept == false && offset.magnitude <= sleepRadius)
            {
                prevPos = transform.position;
                hasSlept = true;
                agent.SetDestination(sleepSpot.position);
                state = HareState.ToSleep;
            }
        }

        private void MoveToSleep()
        {
            if (agent.pathPending) return;
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                SetHareMoving(0f);
                state = HareState.Sleeping;
                Sleep();
            }
        }

        private async UniTaskVoid Sleep()
        {
            animator.SetBool(IsSleeping, true);
            RaceManager.Instance.NotifyHareSleeping();
            await UniTask.WaitForSeconds(attackedDuration, cancellationToken: destroyToken);
            animator.SetBool(IsSleeping, false);
            SetReturnStat();
        }

        private void SetReturnStat()
        {
            SetHareMoving(moveSpeed);
            state = HareState.Returning;
        }

        private void ReturnToTrack()
        {
            agent.SetDestination(prevPos);

            if (agent.pathPending) return;

            if (Vector3.Distance(transform.position, prevPos) > 0.2f) return;
            
            state = HareState.ToGoal;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (sleepTree == null) return;
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(sleepTree.position, Vector3.up, sleepRadius);
        }
#endif
        
        // 애니메이션 단축 메서드
        private void SetHareMoving(float speed)
        {
            agent.speed = speed;
            animator.SetFloat(Speed, agent.speed);
        }
        
        // 넉백 판정
        public void ApplyKnockback(Vector3 explosionPosition)
        {
            if (RaceManager.Instance.CanAttack == false) return;
            if (state == HareState.Sleeping || state == HareState.Attacked ||
                state == HareState.GettingUp) return;
            Rigidbody rBody = GetComponent<Rigidbody>();
            
            // 날아가기 직전 위치
            if (state == HareState.ToGoal)
            {
                prevPos = RaceManager.Instance.IsStage1 ? agent.nextPosition : transform.position;

            }
            
            SetHareMoving(0f);
            state = HareState.Attacked;
            SoundManager.Play(SoundEffect.HareHit);
            
            IsInFlight = true;
            
            agent.enabled = false;
            rBody.isKinematic = ragdollBodies.Length > 0;
            if (ragdollBodies.Length == 0)
                rBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 날아가는 방향 랜덤 설정 - 추후 시간이 남으면 폭발 지점과 현 위치에 맞는 방향으로 날아가게 수정
            Vector3 direction = transform.position - explosionPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = -transform.forward;
                direction.y = 0f;
            }
            direction.Normalize();

            Vector3 impulse = direction * horizontalImpulse + Vector3.up * upwardImpulse;

            if (ragdollBodies.Length > 0)
            {
                SetRagdollActive(true);

                Vector3 spinAxis = Vector3.Cross(Vector3.up, direction);

                Vector3 spinVelocity = spinAxis * spinImpulse;
                Vector3 spinCenter = ragdollHips.worldCenterOfMass;
                for (int i = 0; i < ragdollBodies.Length; i++)
                {
                    Rigidbody boneBody = ragdollBodies[i];

                    Vector3 offset = boneBody.worldCenterOfMass - spinCenter;

                    boneBody.linearVelocity = impulse + Vector3.Cross(spinVelocity, offset);

                    boneBody.maxAngularVelocity = 20f;
                    boneBody.angularVelocity = spinVelocity;
                }
                
                RecoverFromRagdoll();
                return;
            }
            
            rBody.AddForce(impulse, ForceMode.Impulse);
            animator.SetBool(IsSleeping, true);

            RecoverFromKnockBack(rBody);
        }
        
        public void NotifyFlightCollision(Collision collision)
        {
            if (IsInFlight == false) return;

            // 자기 몸의 다른 뼈와 부딪힌 경우는 제외한다.
            if (collision.transform.IsChildOf(transform)) return;

            Rigidbody body = ragdollHips != null ? ragdollHips : rootBody;

            // 날아오르는 순간 발이 바닥에 닿아 있어도 비행을 종료하지 않는다.
            if (body.linearVelocity.y > 0.1f && collision.contactCount > 0 &&
                collision.GetContact(0).normal.y > 0.5f) return;

            IsInFlight = false;
            // 여러 뼈가 동시에 충돌해도 첫 외부 충돌에서만 한 번 재생한다.
            SoundManager.Play(SoundEffect.HareLand);
        }

        private async UniTaskVoid RecoverFromKnockBack(Rigidbody rBody)
        {
            await UniTask.WaitForSeconds(attackedDuration, cancellationToken: destroyToken);
            animator.SetBool(IsSleeping, false);

            // 물리 움직임이 멈출 때까지 대기
            await UniTask.WaitUntil(() => rBody.IsSleeping(), cancellationToken: destroyToken);

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, agent.areaMask) == false)
            {
                Debug.LogWarning("토끼 주변에 복귀 가능한 NavMesh가 없습니다.");
                return;
            }

            rBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rBody.isKinematic = true;
            transform.position = hit.position;

            agent.enabled = true;
            agent.Warp(hit.position);

            SetReturnStat();
        }
    }
}
