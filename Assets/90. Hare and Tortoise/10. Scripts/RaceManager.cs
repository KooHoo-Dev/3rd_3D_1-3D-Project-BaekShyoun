using System.Threading;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;

namespace HareAndTortoise
{
    public enum StageType
    {
        TutorialA,
        TutorialB,
        TutorialC,
        Stage1,
    }
    
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Stage Settings")]
        [SerializeField] private StageType stageType = StageType.TutorialC;
        public bool CanSleep { get; private set; } = true;
        public bool CanAttack { get; private set; } = true;
        public bool HasPreparation { get; private set; } = true;

        [Header("Stage1 Spawn Settings")]
        [SerializeField] private Transform hareTransform;
        [SerializeField] private Transform tortoiseTransform;
        [SerializeField] private Transform hareSpawnPoint;
        [SerializeField] private Transform tortoiseSpawnPoint;
        
        public bool canStartTortoise { get; private set; }
        public bool canStartHare { get; private set; }

        public bool IsPreParing { get; private set; } = true;

        [Header("Prepare Settings")]
        [SerializeField] private float prepareDuration = 60f;
        private bool isRequestToStart;
        private CancellationToken destroyToken;
        
        [Header("UI Settings")]
        [SerializeField] private TMP_Text raceText;

        [Header("Cameras")]
        [SerializeField] private CinemachineCamera[] cameras;
        [SerializeField] private CinemachineBrain mainCam;
        
        // 승리 결정
        private bool canDecideWinner = false;
        private string winnerName;

        
        private void OnTriggerEnter(Collider other)
        {
            if (canDecideWinner) return;
            
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

        private void Awake()
        {
            Instance = this;
            destroyToken = this.GetCancellationTokenOnDestroy();
            CheckStage();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            canStartTortoise = false;
            canStartHare = false;

            if (stageType == StageType.Stage1)
            {
                hareTransform.GetComponent<HareController>().PlaceAtSpawn(hareSpawnPoint);
                tortoiseTransform.GetComponent<TortoiseController>().PlaceAtSpawn(tortoiseSpawnPoint);
            }
            
            TextManager.SetMessage(raceText, true, "Press Enter To Start Race");
            
            IsPreParing = HasPreparation;
            if (HasPreparation)
            {
                SelectCamera(0);
                StartCountDown();
            }
            else SelectCamera(1);
        }

        private void Update()
        {
            if(SimpleInput.GetKeyDown(Key.Enter)) RequestToStart();
            
            
            // 아래는 약간 임시 코드. 추후 UI 연결하면서 제대로 다룰 것
            // + 추후 현재 씬 넘버 고려해서 넘길 수도 있어야 함
            if (canDecideWinner)
            {
                if (winnerName == "거북이")
                {
                    Debug.Log("다음 스테이지로 이동하시겠습니까? ( Y / N )");
                    if (SimpleInput.GetKeyDown(Key.Y)) LoadStage();
                    if (SimpleInput.GetKeyDown(Key.N)) Debug.Log("게임을 종료합니다.-> 편의상 진짜 종료는 아님");
                }

                if (winnerName == "토끼")
                {
                    Debug.Log("재도전하시겠습니까? ( Y / N )");
                    if (SimpleInput.GetKeyDown(Key.Y)) 
                        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    if (SimpleInput.GetKeyDown(Key.N)) Debug.Log("게임을 종료합니다.-> 편의상 진짜 종료는 아님");
                }
            }
            
            // 이건 진짜 임시. 대충 초기화 로직임
            if (SimpleInput.GetKeyDown(Key.Digit0)) SceneManager.LoadScene(0);
            if (SimpleInput.GetKeyDown(Key.Digit1)) SceneManager.LoadScene(1);
        }

        private void CheckStage()
        {
            switch (stageType)
            {
                case StageType.TutorialA:
                    CanSleep = false;
                    CanAttack = false;
                    HasPreparation = false;
                    break;
                case StageType.TutorialB:
                    CanSleep = true;
                    CanAttack = false;
                    HasPreparation = false;
                    break;
                case StageType.TutorialC or StageType.Stage1:
                    CanSleep = false;
                    CanAttack = true;
                    HasPreparation = true;
                    break;
            }
        }

        private void LoadStage()
        {
            switch (stageType)
            {
                case StageType.TutorialA:
                    SceneManager.LoadScene("01. Tutorial B");
                    break;
                case StageType.TutorialB:
                    SceneManager.LoadScene("02. Tutorial C");
                    break;
                case StageType.TutorialC:
                    TextManager.SetMessage(raceText, true, "Gonna be soon");
                    break;
            }
        }
        
        private void RequestToStart()
        {
            if (isRequestToStart || canDecideWinner) return;
            
            isRequestToStart = true;
            if (HasPreparation) return;

            canStartTortoise = true;
            canStartHare = true;
            TextManager.SetMessage(raceText, false);
        }
        
        private async UniTaskVoid StartCountDown()
        {
            float remainTime = prepareDuration;
            while (remainTime > 0f && isRequestToStart == false)
            {
                TextManager.SetMessage(raceText, true, 
                    $"GetReady : {remainTime:F0}seconds \n Enter To Start Now");
                await UniTask.Yield(cancellationToken: destroyToken);
                remainTime -= Time.deltaTime;
            }

            IsPreParing = false;
            
            SelectCamera(1);
            await UniTask.NextFrame(cancellationToken: destroyToken);
            await UniTask.WaitUntil(() => mainCam.IsBlending == false, cancellationToken: destroyToken);

            canStartTortoise = true;
            canStartHare = true;
            Debug.Log("Race Start!");
            TextManager.SetMessage(raceText, true, "Start!");
            await UniTask.WaitForSeconds(1f, cancellationToken: destroyToken);
            TextManager.SetMessage(raceText, false);
        }

        private void SelectCamera(int index)
        {
            if (index < 0 || index >= cameras.Length) return;
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].Priority = (i == index) ? 20 : 10;
            }
        }
    }
}