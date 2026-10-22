using System;
using System.Collections;
using Study.Utilities;
using UnityEditor.Analytics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityPipeline.Microsoft.CodeAnalysis.FlowAnalysis;

namespace HareAndTortoise
{
    public class HareController : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
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
                case HareState.Attacked:
                    break;
                case HareState.Returning:
                    break;
            }
        }
        
        private void MoveToGoal()
        {
            state = HareState.ToGoal;
            agent.speed = 3.5f;
            animator.SetFloat(Speed, agent.speed);
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
                agent.speed = 0f;
                animator.SetFloat(Speed, agent.speed);
                StartCoroutine(Sleep());
            }
        }

        private IEnumerator Sleep()
        {
            yield return null;
        }

        private void OnDrawGizmosSelected()
        {
            if (sleepTree == null) return;
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(sleepTree.position, Vector3.up, sleepRadius);
        }
    }
}