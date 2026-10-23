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
        
        // 업무 단축 메서드
        private void SetHareMoving(float speed)
        {
            agent.speed = speed;
            animator.SetFloat(Speed, agent.speed);
        }
    }
}