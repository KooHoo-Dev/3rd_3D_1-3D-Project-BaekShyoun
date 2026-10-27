using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace HareAndTortoise
{
    public class HareController : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsSleeping = Animator.StringToHash("IsSleeping");

        [Header("Goal Settings")]
        [SerializeField] private Transform goalPoint;
        
        [Header("Sleep Settings")]
        [SerializeField] private Transform sleepTree;
        [SerializeField] private Transform sleepSpot;
        [SerializeField] private float sleepRadius = 3f;
        private Vector3 prevPos;
        private bool hasSlept;
        private HareState state = HareState.ToGoal;

        [Header("Knockback Settings")]
        [SerializeField] private Transform knockbackDirection;
        [SerializeField] private float horizontalImpulse = 8f; // 수평
        [SerializeField] private float upwardImpulse = 5f; // 수직(+)
        [SerializeField] private float attackedDuration = 10f;


        private enum HareState
        {
            ToGoal,
            ToSleep,
            Sleeping,
            Attacked,
            Returning,
        }

        // 컴포넌트
        private NavMeshAgent agent;
        private Animator animator;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            hasSlept = false;
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
                case HareState.Returning:
                    ReturnToTrack();
                    break;
            }
        }
        
        private void MoveToGoal()
        {
            Debug.Log("MoveToGoal Start");
            SetHareMoving(3.5f);
            agent.SetDestination(goalPoint.position);
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
                StartCoroutine(Sleep());
            }
        }

        private IEnumerator Sleep()
        {
            animator.SetBool(IsSleeping, true);
            yield return new WaitForSeconds(10f);
            animator.SetBool(IsSleeping, false);
            SetReturnStat();
        }

        private void SetReturnStat()
        {
            SetHareMoving(3.5f);
            state = HareState.Returning;
        }

        private void ReturnToTrack()
        {
            agent.SetDestination(prevPos);
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                state = HareState.ToGoal;
            }
        }

        private void OnDrawGizmos()
        {
            if (sleepTree == null) return;
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(sleepTree.position, Vector3.up, sleepRadius);
        }
        
        // 애니메이션 단축 메서드
        private void SetHareMoving(float speed)
        {
            agent.speed = speed;
            animator.SetFloat(Speed, agent.speed);
        }
        
        // 넉백 판정
        [ContextMenu("Test Knockback")]
        public void ApplyKnockback()
        {
            if (state == HareState.Sleeping || state == HareState.Attacked) return;
            Rigidbody rBody = GetComponent<Rigidbody>();
            
            // 날아가기 직전 위치
            prevPos = transform.position;
            
            SetHareMoving(0f);
            state = HareState.Attacked;
            
            agent.enabled = false;
            rBody.isKinematic = false;

            // 날아가는 방향 랜덤 설정 - 추후 시간이 남으면 폭발 지점과 현 위치에 맞는 방향으로 날아가게 수정
            float randomAngle = Random.Range(-135f, 135f);
            Vector3 direction = Quaternion.Euler(0f, randomAngle, 0f) * (-transform.forward);
            direction.y = 0;
            direction.Normalize();

            Vector3 impulse = direction * horizontalImpulse + Vector3.up * upwardImpulse;
            rBody.AddForce(impulse, ForceMode.Impulse);
            animator.SetBool(IsSleeping, true);

            StartCoroutine(RecoverFromKnockBack(rBody));
        }

        private IEnumerator RecoverFromKnockBack(Rigidbody rBody)
        {
            yield return new WaitForSeconds(attackedDuration);
            animator.SetBool(IsSleeping, false);

            // 물리 움직임이 멈출 때까지 대기
            yield return new WaitUntil(() => rBody.IsSleeping());

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, agent.areaMask) == false)
            {
                Debug.LogWarning("토끼 주변에 복귀 가능한 NavMesh가 없습니다.");
                yield break;
            }

            rBody.isKinematic = true;
            transform.position = hit.position;

            agent.enabled = true;
            agent.Warp(hit.position);

            SetReturnStat();
        }
    }
}