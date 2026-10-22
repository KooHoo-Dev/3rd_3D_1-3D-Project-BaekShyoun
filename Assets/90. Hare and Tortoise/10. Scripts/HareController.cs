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

        private NavMeshAgent agent;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }
        
        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Digit1)) MoveToGoal();
        }

        private void MoveToGoal()
        {
            Debug.Log("토끼 출발!");
            agent.SetDestination(goalPoint.position);
        }
    }
}