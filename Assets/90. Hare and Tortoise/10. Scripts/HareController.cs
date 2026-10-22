using Study.Utilities;
using UnityEditor.Analytics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace HareAndTortoise
{
    public class HareController : MonoBehaviour
    {
        [SerializeField] private Transform goalPoint;
        [SerializeField] private Transform sleepTree;
        [SerializeField] private Transform sleepSpot;

        private NavMeshAgent agent;
        private Transform prevPos;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }
        
        private void Update()
        {
            if (RaceManager.Instance.canStartHare == false)
            {
                agent.isStopped = true;
                return;
            }
            MoveToGoal();
        }

        private void MoveToGoal()
        {
            agent.isStopped = false;
            agent.SetDestination(goalPoint.position);
        }
    }
}