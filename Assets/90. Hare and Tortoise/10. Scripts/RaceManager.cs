using System.Collections;
using Jay;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor.Searcher;

namespace HareAndTortoise
{
    public class RaceManager : SingletonBase<RaceManager>
    {
        public bool canStartTortoise;
        public bool canStartHare;
        
        [Header("UI Settings")]
        [SerializeField] private TMP_Text raceText;
        
        // 승리 결정
        private bool canDecideWinner = false;
        private string winnerName;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Tortoise"))
            {
                Debug.Log($"거북이 승리!");
                TextManager.SetMessage(raceText, true, "Tortoise Win!\nTry Next Stage( Y / N )");
                canStartHare = false;
                canDecideWinner = true;
                winnerName = "거북이";
            }
            if (other.CompareTag("Hare"))
            {
                Debug.Log($"토끼 승리!");
                TextManager.SetMessage(raceText, true, "Hare Win!\nTry Again( Y / N )");
                canStartTortoise = false;
                canDecideWinner = true;
                winnerName = "토끼";
            }
        }

        private void Start()
        {
            canStartTortoise = false;
            canStartHare = false;
            Debug.Log("Press Enter To Start Race");
            TextManager.SetMessage(raceText, true, "Press Enter To Start Race");
            StartCoroutine(StartCountDown());
        }

        private void Update()
        {
            // 아래는 약간 임시 코드. 추후 UI 연결하면서 제대로 다룰 것
            // + 추후 현재 씬 넘버 고려해서 넘길 수도 있어야 함
            if (canDecideWinner)
            {
                if (winnerName == "거북이")
                {
                    Debug.Log("다음 스테이지로 이동하시겠습니까? ( Y / N )");
                    if (SimpleInput.GetKeyDown(Key.Y)) SceneManager.LoadScene(1);
                    if (SimpleInput.GetKeyDown(Key.N)) Debug.Log("게임을 종료합니다.-> 편의상 진짜 종료는 아님");
                }

                if (winnerName == "토끼")
                {
                    Debug.Log("재도전하시겠습니까? ( Y / N )");
                    if (SimpleInput.GetKeyDown(Key.Y)) SceneManager.LoadScene(0);
                    if (SimpleInput.GetKeyDown(Key.N)) Debug.Log("게임을 종료합니다.-> 편의상 진짜 종료는 아님");
                }
            }
            
            // 이건 진짜 임시. 대충 초기화 로직임
            if (SimpleInput.GetKeyDown(Key.Digit0)) SceneManager.LoadScene(0);
            if (SimpleInput.GetKeyDown(Key.Digit1)) SceneManager.LoadScene(1);
        }
        
        private IEnumerator StartCountDown()
        {
            yield return new WaitUntil(() => SimpleInput.GetKeyDown(Key.Enter));
            for (int i = 3; i > 0; i--)
            {
                Debug.Log(i);
                TextManager.SetMessage(raceText, true, $"{i}");
                yield return new WaitForSeconds(1f);
            }

            canStartTortoise = true;
            canStartHare = true;
            Debug.Log("Race Start!");
            TextManager.SetMessage(raceText, true, "Start!");
            yield return new WaitForSeconds(1f);
            TextManager.SetMessage(raceText, false);
        }
    }
}