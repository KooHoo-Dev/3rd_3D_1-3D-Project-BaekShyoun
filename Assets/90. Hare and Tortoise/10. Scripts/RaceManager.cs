using System.Collections;
using Jay;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HareAndTortoise
{
    public class RaceManager : SingletonBase<RaceManager>
    {
        public bool canStartTortoise;
        public bool canStartHare;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Tortoise"))
            {
                Debug.Log($"거북이 승리!");
                canStartHare = false;
            }
            if (other.CompareTag("Hare"))
            {
                Debug.Log($"토끼 승리!");
                canStartTortoise = false;
            }
        }

        private void Start()
        {
            canStartTortoise = false;
            canStartHare = false;
            StartCoroutine(StartCountDown());
        }
        
        private IEnumerator StartCountDown()
        {
            Debug.Log("Press Enter To Start Race");
            yield return new WaitUntil(() => SimpleInput.GetKeyDown(Key.Enter));
            for (int i = 3; i > 0; i--)
            {
                Debug.Log(i);
                yield return new WaitForSeconds(1f);
            }

            canStartTortoise = true;
            canStartHare = true;
            Debug.Log("Race Start!");
        }
    }
}